using BeauUtil;
using BeauUtil.Debugger;
using System;
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
        public ExportedTileInfo* ExportedTiles;
        public ExportedPaletteEntryInfo* ExportedColors;

        static public bool Initialize(TileExporter* exporter, TileCondenserBuffer* buffer, in TilePackingSettings settings, Unsafe.ArenaHandle allocator) {
            long totalPixels = TileUtility.CalculateTotalExportPixels(buffer, settings);
            if (!TileUtility.CalculateBestTextureSize(totalPixels, out Vector2Int texSize)) {
                Log.Error("[TileExporter] Tiles will not fit in texture of max size ({0}x{0})", TileUtility.MaxExportSize);
                return false;
            }
            exporter->TextureWidth = texSize.x;
            exporter->TextureHeight = texSize.y;
            exporter->Padding = settings.Padding;
            exporter->ExportedTiles = allocator.AllocArray<ExportedTileInfo>(buffer->TileCount);
            exporter->ExportedColors = allocator.AllocArray<ExportedPaletteEntryInfo>(buffer->PaletteEntryCount);
            return true;
        }
    }

    static public unsafe partial class TileUtility {
        public const int MaxExportSize = 8192;
        
        static public bool CalculateBestTextureSize(long totalPixels, out Vector2Int size) {
            long width = 256;
            long height = 256;
            while(true) {
                if (totalPixels <= width * height) {
                    size = new Vector2Int((int)width, (int)height);
                    return true;
                }

                width <<= 1;
                if (width > MaxExportSize) {
                    size = default;
                    return false;
                }

                if (totalPixels <= width * height) {
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

        static public long CalculateTotalExportPixels(TileCondenserBuffer* condenser, in TilePackingSettings settings) {
            long pixelsPerTile = (condenser->TileSize + settings.Padding * 2);
            pixelsPerTile *= pixelsPerTile;
            long pixelsPerColor = settings.PaletteTileSize * settings.PaletteTileSize;
            return condenser->TileCount * pixelsPerTile + condenser->PaletteEntryCount * pixelsPerColor;
        }
        
        static public bool WriteTilesToTexture(TileCondenserBuffer* condenser, TileExporter* exporter, Texture2D output) {
            bool reinitialized = output.Reinitialize(exporter->TextureWidth, exporter->TextureHeight, TextureFormat.RGBA32, false);
            if (!reinitialized) {
                Log.Error("[TileUtility] Issue when reinitializing texture");
                return false;
            }

            int totalTileSize = condenser->TileSize + exporter->Padding * 2;
            int tilesX = exporter->TextureWidth / totalTileSize;
            int tilesY = exporter->TextureHeight / totalTileSize;
            return true;
        }

        static public void CopyTileToTexture(PixelRGBA32* src, int srcCount, int srcWidth, int padding, PixelRGBA32* dst, int dstWidth) {

        }
    }
}