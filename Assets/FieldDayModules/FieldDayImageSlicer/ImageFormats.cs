using BeauUtil;
using System;
using System.Runtime.InteropServices;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;

namespace FieldDay.ImageSlicer {
    public unsafe struct ImageData<T> where T : unmanaged {
        public int Width;
        public int Height;
        public NativeArray<T> PixelDataContainer;
        public T* PixelData;
    }

    static public unsafe partial class TileUtility {
        static public ImageData<T> CreateImageData<T>(Texture2D texture) where T : unmanaged {
            ImageData<T> data;
            data.Width = texture.width;
            data.Height = texture.height;
            data.PixelDataContainer = texture.GetRawTextureData<T>();
            data.PixelData = (T*) data.PixelDataContainer.GetUnsafeReadOnlyPtr();
            return data;
        }

        static public void DisposeImageData<T>(ImageData<T>* data) where T : unmanaged {
            data->PixelDataContainer.Dispose();
        }
        
        static public bool ReadBlock<T>(ImageData<T> image, int x, int y, int width, int height, delegate*<T, PixelRGBA32> converter, PixelRGBA32* output)
            where T : unmanaged {
            if (x >= image.Width || x + width <= 0 || y >= image.Height || y + height <= 0) {
                return false;
            }

            int clipXMin = Math.Max(x, 0);
            int clipYMin = Math.Max(y, 0);
            int clipXMax = Math.Min(image.Width, x + width);
            int clipYMax = Math.Min(image.Height, y + height);

            int emptyRowsPre = clipYMin - y;
            int emptyRowsPost = y + height - clipYMax;
            int emptyColumnsPre = clipXMin - x;
            int emptyColumnsPost = x + width - clipXMax;

            // TODO: clamp x/y for reads

            PixelRGBA32* writeHead = output;
            while(emptyRowsPre-- > 0) {
                Unsafe.Clear(writeHead, width);
                writeHead += width;
            }

            for(int dy = clipYMin; dy < clipYMax; dy++) {
                int cols = emptyColumnsPre;
                while(cols-- > 0) {
                    *writeHead++ = default;
                }

                for(int dx = clipXMin; dx < clipXMax; dx++) {
                    *writeHead++ = converter(image.PixelData[dx + dy * image.Width]);
                }

                cols = emptyColumnsPost;
                while(cols-- > 0) {
                    *writeHead++ = default;
                }
            }

            while(emptyRowsPost-- > 0) {
                Unsafe.Clear(writeHead, width);
                writeHead += width;
            }

            return true;
        }

        static public PixelRGBA32 ReadPixel<T>(ImageData<T> image, int x, int y, delegate*<T, PixelRGBA32> converter)
            where T : unmanaged{
            if (x < 0 || x >= image.Width || y < 0 || y >= image.Height) {
                return default;
            }

            T nativePixel = image.PixelData[x + y * image.Width];
            return converter(nativePixel);
        }
    }
}