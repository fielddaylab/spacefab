using BeauUtil;
using System;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace FieldDay.ImageSlicer {
    public enum ImageBlockType : byte {
        FullyTransparent,
        SingleColor,
        ColorContent
    }

    public struct ImageBlockInfo {
        private const int BufferBits = 30;
        private const uint BufferMask = (1 << BufferBits) - 1;
        private const uint TypeMask = 0x3;

        public uint Raw;

        public ImageBlockInfo(ImageBlockType blockType, uint bufferEntry) {
            Raw = (bufferEntry & BufferMask)
                | (((uint) blockType & TypeMask) << BufferBits);
        }

        public uint BufferEntryIndex {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return Raw & BufferMask; }
        }

        public ImageBlockType Type {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return (ImageBlockType) ((Raw >> BufferBits) & TypeMask); }
        }
    }

    [Serializable]
    public struct TilePackingSettings {
        public int TileSize;
        public int Padding;
        public int PaletteTileSize;
    }

    public unsafe struct ImageInstance {
        public int GridWidth;
        public int GridHeight;
        public int TileSize;
        public int TilePixelCount;
        public int TotalTiles;

        public int ExcessPixelsX;
        public int ExcessPixelsY;

        public int Id;

        static public void Initialize(ImageInstance* instance, int tileSize, Texture2D texture) {
            instance->GridWidth = (texture.width + tileSize - 1) / tileSize;
            instance->GridHeight = (texture.height + tileSize - 1) / tileSize;
            instance->TileSize = tileSize;
            instance->TilePixelCount = tileSize * tileSize;
            instance->TotalTiles = instance->GridWidth * instance->GridHeight;

            instance->ExcessPixelsX = instance->GridWidth * tileSize - texture.width;
            instance->ExcessPixelsY = instance->GridHeight * tileSize - texture.height;

            instance->Id = texture.GetInstanceID();
        }
    }

    public unsafe struct ImageSlicingBuffer {
        public int PixelCapacity;
        public int TileCapacity;
        public int ReadCapacity;

        public ImageBlockInfo* TileInfoGrid;
        public PixelRGBA32* PixelReadBuffer;

        public int TileContentCount;
        public PixelRGBA32* TileContentsBuffer;

        public int SingleColorCount;
        public PixelRGBA32* SingleColorBuffer;

        static public void Initialize(ImageSlicingBuffer* buffer, int maxTextureDimension, int minTileSize, int maxTileSize, Unsafe.ArenaHandle allocator) {
            int maxTilesOnOneSide = (maxTextureDimension + minTileSize - 1) / minTileSize;
            int tileCapacity = maxTilesOnOneSide * maxTilesOnOneSide;
            int pixelCapacity = tileCapacity * minTileSize * minTileSize;

            buffer->PixelCapacity = pixelCapacity;
            buffer->TileCapacity = tileCapacity;
            buffer->ReadCapacity = maxTileSize * maxTileSize;

            buffer->PixelReadBuffer = allocator.AllocArray<PixelRGBA32>(buffer->ReadCapacity);
            buffer->TileInfoGrid = allocator.AllocArray<ImageBlockInfo>(tileCapacity);
            buffer->TileContentsBuffer = allocator.AllocArray<PixelRGBA32>(pixelCapacity);
            buffer->SingleColorBuffer = allocator.AllocArray<PixelRGBA32>(tileCapacity);
        }

        static public void Prepare(ImageSlicingBuffer* buffer, ImageInstance* instance) {
            if (instance->TilePixelCount > buffer->ReadCapacity) {
                throw new InvalidOperationException("tile size too big for slicing buffer");
            }

            if (instance->TotalTiles > buffer->TileCapacity) {
                throw new InvalidOperationException("too many tiles for slicing buffer");
            }

            if (instance->TotalTiles * instance->TilePixelCount > buffer->PixelCapacity) {
                throw new InvalidOperationException("total texture size too big for slicing buffer");
            }

            buffer->TileContentCount = 0;
            buffer->SingleColorCount = 0;
            Unsafe.Clear(buffer->TileInfoGrid, buffer->TileCapacity);
        }

        static public void ReadTileIntoBuffer<T>(ImageSlicingBuffer* buffer, ImageInstance* instance, ImageData<T> data, int tileIndex, delegate*<T, PixelRGBA32> converter) where T : unmanaged {
            int size = instance->TileSize;
            int x = (tileIndex % instance->GridWidth) * size;
            int y = (tileIndex / instance->GridWidth) * size;
            TileUtility.ReadBlock<T>(data, x, y, size, size, converter, buffer->PixelReadBuffer);
        }

        static public void ProcessCurrentTile(ImageSlicingBuffer* buffer, ImageInstance* instance, int tileIndex) {
            ImageBlockType blockType = TileUtility.CalculateTileType(buffer->PixelReadBuffer, instance->TilePixelCount);
            switch (blockType) {
                case ImageBlockType.FullyTransparent: {
                    WriteTileInfo(buffer, tileIndex, new ImageBlockInfo(blockType, 0));
                    break;
                }

                case ImageBlockType.SingleColor: {
                    int colorIndex = CommitSingleColorFromReadBuffer(buffer);
                    WriteTileInfo(buffer, tileIndex, new ImageBlockInfo(blockType, (uint) colorIndex));
                    break;
                }

                case ImageBlockType.ColorContent: {
                    int contentIndex = CommitContentTileFromReadBuffer(buffer, instance);
                    WriteTileInfo(buffer, tileIndex, new ImageBlockInfo(blockType, (uint) contentIndex));
                    break;
                }
            }
        }

        static public int CommitSingleColor(ImageSlicingBuffer* buffer, PixelRGBA32 color) {
            if (buffer->SingleColorCount >= buffer->TileCapacity) {
                throw new IndexOutOfRangeException("no more room in color buffer");
            }
            int index = buffer->SingleColorCount++;
            buffer->SingleColorBuffer[index] = color;
            return index;
        }

        static public int CommitSingleColorFromReadBuffer(ImageSlicingBuffer* buffer) {
            return CommitSingleColor(buffer, buffer->PixelReadBuffer[0]);
        }

        static public int CommitContentTile(ImageSlicingBuffer* buffer, ImageInstance* instance, PixelRGBA32* readBuffer) {
            int stride = instance->TilePixelCount;
            if (buffer->TileContentCount >= buffer->TileCapacity) {
                throw new IndexOutOfRangeException("no more room in tile buffer");
            }
            int index = buffer->TileContentCount++;
            Unsafe.FastCopyArray(readBuffer, stride, buffer->TileContentsBuffer + stride * index);
            return index;
        }

        static public int CommitContentTileFromReadBuffer(ImageSlicingBuffer* buffer, ImageInstance* instance) {
            return CommitContentTile(buffer, instance, buffer->PixelReadBuffer);
        }
    
        static public void WriteTileInfo(ImageSlicingBuffer* buffer, int index, ImageBlockInfo tile) {
            buffer->TileInfoGrid[index] = tile;
        }
    }

    static public unsafe partial class TileUtility {
        static public bool ProcessImageIntoSlices(ImageSlicingBuffer* buffer, Texture2D texture, TilePackingSettings settings, out ImageInstance outputStats) {
            ImageInstance instance;
            ImageInstance.Initialize(&instance, settings.TileSize, texture);
            ImageSlicingBuffer.Prepare(buffer, &instance);
            switch (texture.format) {
                case TextureFormat.RGB24: {
                    ImageData<PixelRGB24> texData = CreateImageData<PixelRGB24>(texture);
                    ProcessImageData(buffer, &instance, texData);
                    DisposeImageData(&texData);
                    break;
                }
                case TextureFormat.ARGB32: {
                    ImageData<PixelARGB32> texData = CreateImageData<PixelARGB32>(texture);
                    ProcessImageData(buffer, &instance, texData);
                    DisposeImageData(&texData);
                    break;
                }

                case TextureFormat.RGBA32: {
                    ImageData<PixelRGBA32> texData = CreateImageData<PixelRGBA32>(texture);
                    ProcessImageData(buffer, &instance, texData);
                    DisposeImageData(&texData);
                    break;
                }

                default: {
                    Debug.LogErrorFormat("unable to process image format {0}", texture.format.ToString());
                    outputStats = default;
                    return false;
                }
            }

            outputStats = instance;
            return true;
        }

        static private void ProcessImageData(ImageSlicingBuffer* buffer, ImageInstance* instance, ImageData<PixelARGB32> image) {
            int tileCount = instance->TotalTiles;
            for (int i = 0; i < tileCount; i++) {
                ImageSlicingBuffer.ReadTileIntoBuffer(buffer, instance, image, i, &TileUtility.ARGB32ToRGBA32);
                ImageSlicingBuffer.ProcessCurrentTile(buffer, instance, i);
            }
        }

        static private void ProcessImageData(ImageSlicingBuffer* buffer, ImageInstance* instance, ImageData<PixelRGB24> image) {
            int tileCount = instance->TotalTiles;
            for (int i = 0; i < tileCount; i++) {
                ImageSlicingBuffer.ReadTileIntoBuffer(buffer, instance, image, i, &TileUtility.RGB24ToRGBA32);
                ImageSlicingBuffer.ProcessCurrentTile(buffer, instance, i);
            }
        }

        static private void ProcessImageData(ImageSlicingBuffer* buffer, ImageInstance* instance, ImageData<PixelRGBA32> image) {
            int tileCount = instance->TotalTiles;
            for (int i = 0; i < tileCount; i++) {
                ImageSlicingBuffer.ReadTileIntoBuffer(buffer, instance, image, i, &TileUtility.RGBA32Identity);
                ImageSlicingBuffer.ProcessCurrentTile(buffer, instance, i);
            }
        }

        /// <summary>
        /// Determines the type of tile for the given block of pixel data.
        /// This will return if the block is fully transparent, contains the same color, or contains differing pixels.
        /// </summary>
        static public ImageBlockType CalculateTileType(PixelRGBA32* block, int count) {
            if (count <= 0) {
                return ImageBlockType.FullyTransparent;
            }

            PixelRGBA32* readHead = block;

            PixelRGBA32 first = *readHead++;
            bool fullyTransparent = first.A < 2;
            bool sameColor = true;

            count--;

            while(count-- > 0) {
                PixelRGBA32 pixel = *readHead++;
                fullyTransparent &= pixel.A < 2;
                sameColor &= PixelRGBA32.ApproximatelyEquals(pixel, first);
            }

            if (fullyTransparent) {
                return ImageBlockType.FullyTransparent;
            }
            if (sameColor) {
                return ImageBlockType.SingleColor;
            }
            return ImageBlockType.ColorContent;
        }
    }
}