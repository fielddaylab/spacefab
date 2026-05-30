using BeauUtil;
using BeauUtil.Debugger;
using System;
using Unity.Collections;
using UnityEngine;

namespace FieldDay.ImageSlicer {
    public struct ExportedTileInfo {
        public float U0;
        public float U1;
        public float V0;
        public float V1;
    }

    public struct ExportedPaletteEntryInfo {
        public float U;
        public float V;
    }

    public unsafe struct TileExporter {
        public int TextureWidth;
        public int TextureHeight;
        public int Padding;
        public int PaddedTileSize;
        public int TilesPerRow;

        public int PaletteEntrySize;
        public int PaletteEntriesPerPaddedTile;
        public int PaletteEntriesPerPaddedTileRow;
        
        public ExportedTileInfo* ExportedTiles;
        public ExportedPaletteEntryInfo* ExportedColors;

        static public bool Initialize(TileExporter* exporter, TileCondenserBuffer* buffer, in TilePackingSettings settings, Unsafe.ArenaHandle allocator) {
            int totalTiles = TileUtility.CalculateTotalTiles(buffer, settings);
            int paddedTileSize = TileUtility.CalculatePaddedTileSize(buffer, settings);

            if (!TileUtility.CalculateBestTextureSize(totalTiles, paddedTileSize, out Vector2Int texSize)) {
                Log.Error("[TileExporter] Tiles will not fit in texture of max size ({0}x{0})", TileUtility.MaxExportSize);
                return false;
            }

            exporter->TextureWidth = texSize.x;
            exporter->TextureHeight = texSize.y;
            exporter->Padding = settings.Padding;
            exporter->PaddedTileSize = paddedTileSize;
            exporter->TilesPerRow = texSize.x / paddedTileSize;

            exporter->PaletteEntrySize = settings.PaletteTileSize;
            exporter->PaletteEntriesPerPaddedTileRow = (paddedTileSize / settings.PaletteTileSize);
            exporter->PaletteEntriesPerPaddedTile = exporter->PaletteEntriesPerPaddedTileRow * exporter->PaletteEntriesPerPaddedTileRow;
            
            exporter->ExportedTiles = allocator.AllocArray<ExportedTileInfo>(buffer->TileCount);
            exporter->ExportedColors = allocator.AllocArray<ExportedPaletteEntryInfo>(buffer->PaletteEntryCount);
            return true;
        }
    }

    static public unsafe partial class TileUtility {
        public const int MaxExportSize = 8192;
        
        static public bool CalculateBestTextureSize(int totalTiles, int tileSize, out Vector2Int size) {
            int width = 256;
            int height = 256;
            int supportedTiles;
            while(true) {
                supportedTiles = (width / tileSize) * (height / tileSize);
                if (totalTiles <= supportedTiles) {
                    size = new Vector2Int((int)width, (int)height);
                    return true;
                }

                width <<= 1;
                if (width > MaxExportSize) {
                    size = default;
                    return false;
                }

                supportedTiles = (width / tileSize) * (height / tileSize);
                if (totalTiles <= supportedTiles) {
                    size = new Vector2Int((int)width, (int)height);
                    return true;
                }

                height <<= 1;
                if (height > MaxExportSize) {
                    size = default;
                    return false;
                }
            }
        }

        static public int CalculatePaddedTileSize(TileCondenserBuffer* condenser, in TilePackingSettings settings) {
            return (condenser->TileSize + settings.Padding * 2);
        }

        static public int CalculateTotalTiles(TileCondenserBuffer* condenser, in TilePackingSettings settings) {
            int paddedTileSize = CalculatePaddedTileSize(condenser, settings);
            int paletteTilesPerPaddedTile = paddedTileSize / settings.PaletteTileSize;
            paletteTilesPerPaddedTile *= paletteTilesPerPaddedTile;

            int paletteTiles = (condenser->PaletteEntryCount + paletteTilesPerPaddedTile - 1) / paletteTilesPerPaddedTile;
            return condenser->TileCount * paletteTiles;
        }

        static public Texture2D CreateExportTexture(TileExporter* exporter) {
            return new Texture2D(exporter->TextureWidth, exporter->TextureHeight, TextureFormat.RGBA32, false);
        }
        
        static public bool WriteTilesToTexture(TileCondenserBuffer* condenser, TileExporter* exporter, Texture2D output) {
            int totalTileSize = exporter->PaddedTileSize;
            int tilesX = exporter->TilesPerRow;

            NativeArray<PixelRGBA32> outputPixels = output.GetRawTextureData<PixelRGBA32>();
            int pixelCount = outputPixels.Length;
            using (outputPixels) {
                PixelRGBA32* dstPixels = Unsafe.NativePointer(outputPixels);

                // clear to transparency
                Unsafe.Clear(dstPixels, pixelCount);

                int texWidth = exporter->TextureWidth;
                int texHeight = exporter->TextureHeight;

                int tileSize = condenser->TileSize;
                int paddedTileSize = exporter->PaddedTileSize;
                int paletteSize = exporter->PaletteEntrySize;
                int padding = exporter->Padding;

                int tilesPerRow = exporter->TilesPerRow;

                // single colors first, to top right
                int paletteEntryCount = condenser->PaletteEntryCount;
                for(int i = 0; i < paletteEntryCount; i++) {
                    PixelRGBA32 paletteColor = condenser->PaletteEntries[i];
                    int paddedTileIndex = i / exporter->PaletteEntriesPerPaddedTile;

                    int tileX = paddedTileIndex % tilesPerRow;
                    int tileY = paddedTileIndex / tilesPerRow;

                    int subTileIndex = i % exporter->PaletteEntriesPerPaddedTile;
                    int subTileX = subTileIndex % exporter->PaletteEntriesPerPaddedTileRow;
                    int subTileY = subTileIndex / exporter->PaletteEntriesPerPaddedTileRow;

                    int pixelX = texWidth - tileX * paddedTileSize - (subTileX + 1) * paletteSize;
                    int pixelY = texHeight - tileY * paddedTileSize - (subTileY + 1) * paletteSize;

                    ExportedPaletteEntryInfo entryInfo;
                    entryInfo.U = (pixelX + paletteSize * 0.5f) / (float)texWidth;
                    entryInfo.V = (pixelY + paletteSize * 0.5f) / (float)texHeight;
                    exporter->ExportedColors[i] = entryInfo;

                    FillPaletteRegionInTexture(paletteColor, paletteSize, dstPixels, texWidth, pixelX, pixelY);
                }

                // content tiles next, bottom left
                int contentTIleCount = condenser->TileCount;
                int contentTilePixelCount = condenser->TilePixelSize;
                for (int i = 0; i < contentTIleCount; i++) {
                    PixelRGBA32* tileData = condenser->TileColorBuffer + i * contentTilePixelCount;

                    int pixelX = (i % tilesPerRow) * paddedTileSize;
                    int pixelY = (i / tilesPerRow) * paddedTileSize;

                    ExportedTileInfo entryInfo;
                    entryInfo.U0 = (pixelX + padding) / (float)texWidth;
                    entryInfo.U1 = (pixelX + padding + tileSize) / (float)texWidth;
                    entryInfo.V0 = (pixelY + padding) / (float)texHeight;
                    entryInfo.V1 = (pixelY + padding + tileSize) / (float)texHeight;
                    exporter->ExportedTiles[i] = entryInfo;

                    CopyTileToTexture(tileData, contentTilePixelCount, tileSize, padding, dstPixels, texWidth, pixelX, pixelY);
                }
            }

            return true;
        }

        static public void CopyTileToTexture(PixelRGBA32* src, int srcCount, int srcWidth, int padding, PixelRGBA32* dst, int dstWidth, int dstX, int dstY) {
            CopyTileToTexture(src, srcCount, srcWidth, padding, dst + dstX + dstY * dstWidth, dstWidth);
        }

        static private readonly PixelRGBA32 DebugPixel = new PixelRGBA32(255, 0, 243, 255);

        static public void CopyTileToTexture(PixelRGBA32* src, int srcCount, int srcWidth, int padding, PixelRGBA32* dst, int dstWidth) {
            PixelRGBA32* row = dst + padding;

            int rowWidth = srcWidth;
            int writeCount;

            PixelRGBA32* writeHead;
            PixelRGBA32* readHead = src;

            // tile data copy

            row = dst + padding + (padding * dstWidth);
            int rows = srcWidth;
            while (rows-- > 0) {
                writeHead = row;
                writeCount = rowWidth;
                while (writeCount-- > 0) {
                    *writeHead++ = *readHead++;
                }
                row += dstWidth;
            }

            // bottom padding

            row = dst + padding;
            rows = padding;
            while (rows-- > 0) {
                writeHead = row;
                writeCount = rowWidth;
                readHead = src;
                while (writeCount-- > 0) {
                    *writeHead++ = *readHead++;
                }
                row += dstWidth;
            }

            // top padding

            row = dst + padding + (padding + srcWidth) * dstWidth;
            rows = padding;
            while (rows-- > 0) {
                writeHead = row;
                writeCount = rowWidth;
                readHead = src + (srcWidth - 1) * srcWidth;
                while (writeCount-- > 0) {
                    *writeHead++ = *readHead++;
                }
                row += dstWidth;
            }

            // left padding

            row = dst + (padding * dstWidth);
            rows = srcWidth;
            readHead = src;
            while(rows-- > 0) {
                PixelRGBA32 left = *readHead;
                writeHead = row;
                writeCount = padding;
                while(writeCount-- > 0) {
                    *writeHead++ = left;
                }

                readHead += srcWidth;
                row += dstWidth;
            }

            // right padding

            row = dst + (padding * dstWidth) + (padding + srcWidth);
            rows = srcWidth;
            readHead = src + srcWidth - 1;
            while (rows-- > 0) {
                PixelRGBA32 right = *readHead;
                writeHead = row;
                writeCount = padding;
                while (writeCount-- > 0) {
                    *writeHead++ = right;
                }

                readHead += srcWidth;
                row += dstWidth;
            }
        }

        static public void FillPaletteRegionInTexture(PixelRGBA32 paletteColor, int paletteRegionSize, PixelRGBA32* dst, int dstWidth, int dstX, int dstY) {
            FillPaletteRegionInTexture(paletteColor, paletteRegionSize, dst + dstX + dstY * dstWidth, dstWidth);
        }

        static public void FillPaletteRegionInTexture(PixelRGBA32 paletteColor, int paletteRegionSize, PixelRGBA32* dst, int dstWidth) {
            PixelRGBA32* row = dst;
            
            int rowWidth = paletteRegionSize;

            PixelRGBA32* writeHead;
            int writeCount;
            int rows = paletteRegionSize;
            while(rows-- > 0) {
                writeHead = row;
                writeCount = rowWidth;
                while(writeCount-- > 0) {
                    *writeHead++ = paletteColor;
                }
                row += dstWidth;
            }
        }
    }
}