using UnityEngine;

namespace EL2_cheat_engine
{
	public static class TextureFactory
	{
		public static Texture2D MakeTex(int width, int height, Color color)
		{
			Color[] pixels = new Color[width * height];
			for (int i = 0; i < pixels.Length; i++)
			{
				pixels[i] = color;
			}

			Texture2D texture = new Texture2D(width, height);
			texture.SetPixels(pixels);
			texture.Apply();
			return texture;
		}
	}
}
