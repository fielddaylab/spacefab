using BeauUtil;
using BeauUtil.Debugger;
using System;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace FieldDay.ImageSlicer {
    public struct OrientedTileHashes {
        public ulong Default;
        public ulong FlipX;
        public ulong FlipY;
        public ulong FlipXY;
    }

    public struct CondensedTileReference {
        public int TileIndex;
        public TileTransform Transform;
    }

    public struct TileReuseStats {
        public uint DirectReuse;
        public uint TransformedReuse;
    }

    public struct TileCondenserStats {
        public uint TotalUniqueContentTiles;
        public uint TotalUniquePaletteTiles;

        public uint TotalContentReused;
        public uint TotalContentReusedTransformed;
    }

    public struct TileCondenserTransferStats {
        public uint TotalProcessedTiles;
        public uint TotalPaletteTiles;
        public uint TotalContentTiles;
        public uint TotalEmptyTiles;
    }

    [Flags]
    public enum TileTransform : byte {
        Default = 0,
        FlipX = 0x1,
        FlipY = 0x02,
        FlipXY = FlipX | FlipY
    }

    public struct CondensedMeshTile {
        public int TileIndex;
        public ushort X;
        public ushort Y;
        public TileTransform Transform;
        public bool IsSingleColor;
    }

    public unsafe struct CondensedMesh {
        public int Id;
        public int TotalWidth;
        public int TotalHeight;
        public int ExcessPixelsX;
        public int ExcessPixelsY;
        public int TileCount;
        public CondensedMeshTile* Tiles;
        public TileCondenserBuffer* Condenser;
    }

    public unsafe struct TileCondenserBuffer {
        public int TileCapacity;
        public int TileSize;
        public int TilePixelSize;
        public int PaletteCapacity;

        public int TileCount;
        public PixelRGBA32* TileColorBuffer;
        public OrientedTileHashes* TileHashBuffer;
        public TileReuseStats* TileStatsBuffer;

        public int PaletteEntryCount;
        public PixelRGBA32* PaletteEntries;

        public PixelRGBA32* HashScratchBuffer;

        static public void Initialize(TileCondenserBuffer* buffer, int maxTiles, int maxPaletteEntries, int tileSize, Unsafe.ArenaHandle allocator) {
            buffer->TileSize = tileSize;
            buffer->TilePixelSize = tileSize * tileSize;
            buffer->TileCapacity = maxTiles;
            buffer->PaletteCapacity = maxPaletteEntries;

            buffer->TileCount = 0;
            buffer->TileColorBuffer = allocator.AllocArray<PixelRGBA32>(buffer->TilePixelSize * maxTiles);
            buffer->TileHashBuffer = allocator.AllocArray<OrientedTileHashes>(maxTiles);
            buffer->TileStatsBuffer = allocator.AllocArray<TileReuseStats>(maxTiles);

            buffer->PaletteEntryCount = 0;
            buffer->PaletteEntries = allocator.AllocArray<PixelRGBA32>(maxPaletteEntries);

            buffer->HashScratchBuffer = (PixelRGBA32*) allocator.AllocAligned((sizeof(PixelRGBA32) * buffer->TilePixelSize), 8);
        }

        static public int CommitPaletteEntry(TileCondenserBuffer* buffer, PixelRGBA32 singleColor) {
            for(int i = 0; i < buffer->PaletteEntryCount; i++) {
                if (PixelRGBA32.ApproximatelyEquals(buffer->PaletteEntries[i], singleColor)) {
                    return i;
                }
            }

            if (buffer->PaletteEntryCount >= buffer->PaletteCapacity) {
                throw new InvalidOperationException("No more room in palette buffer");
            }

            int index = buffer->PaletteEntryCount++;
            buffer->PaletteEntries[index] = singleColor;
            return index;
        }

        static public CondensedTileReference CommitContentTile(TileCondenserBuffer* buffer, PixelRGBA32* pixels, int count) {
            if (count != buffer->TilePixelSize) {
                throw new InvalidOperationException("Tile sizes do not match!");
            }

            Unsafe.FastCopyArray(pixels, count, buffer->HashScratchBuffer);
            ulong tileHash = TileUtility.ComputeHash(buffer->HashScratchBuffer, count);

            int tileSize = buffer->TileSize;

            bool isFlippedX = false, isFlippedY = false;

            for(int i = 0; i < buffer->TileCount; i++) {
                OrientedTileHashes hashes = buffer->TileHashBuffer[i];
                if (hashes.Default == tileHash) {
                    PerformFlips(buffer->HashScratchBuffer, count, tileSize, isFlippedX, isFlippedY);
                    isFlippedX = isFlippedY = false;
                    if (TileUtility.AreIdentical(buffer->HashScratchBuffer, buffer->TileColorBuffer + i * count, count)) {
                        buffer->TileStatsBuffer[i].DirectReuse++;
                        return new CondensedTileReference() {
                            TileIndex = i,
                            Transform = TileTransform.Default
                        };
                    }
                }

                if (hashes.FlipX == tileHash) {
                    PerformFlips(buffer->HashScratchBuffer, count, tileSize, !isFlippedX, isFlippedY);
                    isFlippedX = true;
                    isFlippedY = false;
                    if (TileUtility.AreIdentical(buffer->HashScratchBuffer, buffer->TileColorBuffer + i * count, count)) {
                        buffer->TileStatsBuffer[i].TransformedReuse++;
                        return new CondensedTileReference() {
                            TileIndex = i,
                            Transform = TileTransform.FlipX
                        };
                    }
                }

                if (hashes.FlipXY == tileHash) {
                    PerformFlips(buffer->HashScratchBuffer, count, tileSize, !isFlippedX, !isFlippedY);
                    isFlippedX = true;
                    isFlippedY = true;
                    if (TileUtility.AreIdentical(buffer->HashScratchBuffer, buffer->TileColorBuffer + i * count, count)) {
                        buffer->TileStatsBuffer[i].TransformedReuse++;
                        return new CondensedTileReference() {
                            TileIndex = i,
                            Transform = TileTransform.FlipXY
                        };
                    }
                }

                if (hashes.FlipY == tileHash) {
                    PerformFlips(buffer->HashScratchBuffer, count, tileSize, isFlippedX, !isFlippedY);
                    isFlippedX = false;
                    isFlippedY = true;
                    if (TileUtility.AreIdentical(buffer->HashScratchBuffer, buffer->TileColorBuffer + i * count, count)) {
                        buffer->TileStatsBuffer[i].TransformedReuse++;
                        return new CondensedTileReference() {
                            TileIndex = i,
                            Transform = TileTransform.FlipY
                        };
                    }
                }
            }
        
            if (buffer->TileCount >= buffer->TileCapacity) {
                throw new InvalidOperationException("No more room in tile buffer");
            }

            int index = buffer->TileCount++;
            Unsafe.FastCopyArray(pixels, count, buffer->TileColorBuffer + index * count);
            buffer->TileHashBuffer[index] = TileUtility.ComputeOrientedHashes(buffer, pixels, count, tileSize);
            buffer->TileStatsBuffer[index] = default;
            return new CondensedTileReference() {
                TileIndex = index,
                Transform = TileTransform.Default
            };
        }

        static private void PerformFlips(PixelRGBA32* pixels, int count, int tileSize, bool flipX, bool flipY) {
            if (flipX && flipY) {
                TileUtility.FlipXY(pixels, count, tileSize);
            } else if (flipX) {
                TileUtility.FlipX(pixels, count, tileSize);
            } else if (flipY) {
                TileUtility.FlipY(pixels, count, tileSize);
            }
        }
    }

    static public unsafe partial class TileUtility {
        static public bool CondenseTilesFromSlices(ImageSlicingBuffer* buffer, ImageInstance* instance, TileCondenserBuffer* condenser, Unsafe.ArenaHandle allocator, ref TileCondenserTransferStats stats, out CondensedMesh result) {
            int tilesToAllocate = buffer->TileContentCount + buffer->SingleColorCount;
            result.TotalWidth = (instance->GridWidth * instance->TileSize);
            result.TotalHeight = (instance->GridHeight * instance->TileSize);
            result.ExcessPixelsX = instance->ExcessPixelsX;
            result.ExcessPixelsY = instance->ExcessPixelsY;

            result.TileCount = tilesToAllocate;
            result.Tiles = allocator.AllocArray<CondensedMeshTile>(tilesToAllocate);
            result.Condenser = condenser;
            result.Id = instance->Id;

            stats.TotalProcessedTiles += (uint) instance->TotalTiles;

            CondensedMeshTile* writeTile = result.Tiles;
            for(int i = 0; i < instance->TotalTiles; i++) {
                ImageBlockInfo blockInfo = buffer->TileInfoGrid[i];
                if (blockInfo.Type == ImageBlockType.FullyTransparent) {
                    stats.TotalEmptyTiles++;
                    continue;
                }

                writeTile->X = (ushort) ((i % instance->GridWidth) * condenser->TileSize);
                writeTile->Y = (ushort) ((i / instance->GridWidth) * condenser->TileSize);
                writeTile->IsSingleColor = blockInfo.Type == ImageBlockType.SingleColor;

                if (blockInfo.Type == ImageBlockType.SingleColor) {
                    int singleColorIndex = TileCondenserBuffer.CommitPaletteEntry(condenser, buffer->SingleColorBuffer[blockInfo.BufferEntryIndex]);
                    writeTile->TileIndex = singleColorIndex;
                    writeTile->Transform = TileTransform.Default;
                    stats.TotalPaletteTiles++;
                } else {
                    CondensedTileReference tileRef = TileCondenserBuffer.CommitContentTile(condenser, buffer->TileContentsBuffer + blockInfo.BufferEntryIndex * instance->TilePixelCount, instance->TilePixelCount);
                    writeTile->TileIndex = tileRef.TileIndex;
                    writeTile->Transform = tileRef.Transform;
                    stats.TotalContentTiles++;
                }

                writeTile++;
            }

            Assert.True(writeTile == result.Tiles + tilesToAllocate, "Wrote incorrect number of tiles out!");

            return true;
        }

        static public ulong ComputeHash(PixelRGBA32* pixels, int count) {
            return Unsafe.Hash64(pixels, sizeof(PixelRGBA32) * count);
        }

        static public bool AreIdentical(PixelRGBA32* a, PixelRGBA32* b, int count) {
            while(count-- > 0) {
                if (!ColorComparison(*a++, *b++)) {
                    return false;
                }
            }
            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static public bool ColorComparison(in PixelRGBA32 a, in PixelRGBA32 b) {
            //return PixelRGBA32.ApproximatelyEquals(a, b);
            return PixelRGBA32.Equals(a, b);
        }

        static public OrientedTileHashes ComputeOrientedHashes(TileCondenserBuffer* buffer, PixelRGBA32* pixels, int count, int tileSize) {
            PixelRGBA32* tempPixelBuffer = buffer->HashScratchBuffer;

            Unsafe.FastCopyArray(pixels, count, tempPixelBuffer);

            OrientedTileHashes hashes;
            hashes.Default = Unsafe.Hash64(tempPixelBuffer, sizeof(PixelRGBA32) * count);

            FlipX(tempPixelBuffer, count, tileSize);
            hashes.FlipX = Unsafe.Hash64(tempPixelBuffer, sizeof(PixelRGBA32) * count);

            FlipY(tempPixelBuffer, count, tileSize);
            hashes.FlipXY = Unsafe.Hash64(tempPixelBuffer, sizeof(PixelRGBA32) * count);

            FlipX(tempPixelBuffer, count, tileSize);
            hashes.FlipY = Unsafe.Hash64(tempPixelBuffer, sizeof(PixelRGBA32) * count);

            return hashes;
        }

        static public void FlipX(PixelRGBA32* pixels, int count, int tileSize) {
            for(int i = 0; i < tileSize; i++) {
                ReversePixels(pixels + i * tileSize, tileSize, 1);
            }
        }

        static public void FlipY(PixelRGBA32* pixels, int count, int tileSize) {
            for (int i = 0; i < tileSize; i++) {
                ReversePixels(pixels + i, tileSize, tileSize);
            }
        }

        static public void FlipXY(PixelRGBA32* pixels, int count, int tileSize) {
            for(int i = 0; i < tileSize; i++) {
                ReversePixels(pixels + i * tileSize, tileSize, 1);
                ReversePixels(pixels + i, tileSize, tileSize);
            }
        }

        static public void ReversePixels(PixelRGBA32* pixels, int count, int stride) {
            PixelRGBA32* low = pixels;
            PixelRGBA32* high = pixels + (count - 1) * stride;

            while(low < high) {
                PixelRGBA32 temp = *low;
                *low = *high;
                *high = temp;
                low += stride;
                high -= stride;
            }
        }
    
        static public void ComputeCondenserStats(TileCondenserBuffer* buffer, out TileCondenserStats stats) {
            stats.TotalUniqueContentTiles = (uint) buffer->TileCount;
            stats.TotalUniquePaletteTiles = (uint) buffer->PaletteEntryCount;

            uint reuseCount = 0, transformCount = 0;
            for(int i = 0; i < buffer->TileCount; i++) {
                TileReuseStats reuse = buffer->TileStatsBuffer[i];
                if (reuse.DirectReuse > 0) {
                    reuseCount++;
                }
                if (reuse.TransformedReuse > 0) {
                    transformCount++;
                }
            }

            stats.TotalContentReused = reuseCount;
            stats.TotalContentReusedTransformed = transformCount;
        }
    
        static public Rect ComputeMeshBounds(in CondensedMesh mesh, float pixelsPerUnit, Vector2 pivot) {
            float worldWidth = mesh.TotalWidth / pixelsPerUnit;
            float worldHeight = mesh.TotalHeight / pixelsPerUnit;

            float x = (worldWidth - mesh.ExcessPixelsX / pixelsPerUnit) * -pivot.x;
            float y = (worldHeight - mesh.ExcessPixelsY / pixelsPerUnit) * -pivot.y;

            return new Rect(x, y, worldWidth, worldHeight);
        }

        static public Vector4 ComputeMeshTileTexCoords(in CondensedMeshTile meshTile, TileExporter* exporter) {
            if (meshTile.IsSingleColor) {
                ExportedPaletteEntryInfo paletteEntry = exporter->ExportedColors[meshTile.TileIndex];
                return new Vector4(paletteEntry.U, paletteEntry.V, paletteEntry.U, paletteEntry.V);
            } else {
                ExportedTileInfo tileInfo = exporter->ExportedTiles[meshTile.TileIndex];
                float left, right, bottom, top;
                
                if ((meshTile.Transform & TileTransform.FlipX) == 0) {
                    left = tileInfo.U0;
                    right = tileInfo.U1;
                } else {
                    left = tileInfo.U1;
                    right = tileInfo.U0;
                }

                if ((meshTile.Transform & TileTransform.FlipY) == 0) {
                    bottom = tileInfo.V0;
                    top = tileInfo.V1;
                } else {
                    bottom = tileInfo.V1;
                    top = tileInfo.V0;
                }

                return new Vector4(left, bottom, right, top);
            }
        }

        static public Vector4 ComputeMeshTilePositions(in CondensedMesh mesh, in CondensedMeshTile meshTile) {
            float tileSize = mesh.Condenser->TileSize;
            float x0 = (float)meshTile.X / mesh.TotalWidth;
            float y0 = (float)meshTile.Y / mesh.TotalHeight;
            float x1 = (float)(meshTile.X + tileSize) / mesh.TotalWidth;
            float y1 = (float)(meshTile.Y + tileSize) / mesh.TotalHeight;
            return new Vector4(x0, y0, x1, y1);
        }
    }
}