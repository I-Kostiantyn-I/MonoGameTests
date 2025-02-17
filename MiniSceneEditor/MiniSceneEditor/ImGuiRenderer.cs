using ImGuiNET;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

public class ImGuiRenderer : IDisposable
{
	private Game _game;
	private GraphicsDevice _graphicsDevice;
	private BasicEffect _effect;
	private RasterizerState _rasterizerState;
	private VertexBuffer _vertexBuffer;
	private IndexBuffer _indexBuffer;
	private int _vertexBufferSize;
	private int _indexBufferSize;
	private Dictionary<IntPtr, Texture2D> _loadedTextures;
	private int _textureId;
	private IntPtr _fontTextureId;
	private Texture2D _fontTexture;
	private int _scrollWheelValue;
	private readonly Dictionary<Keys, int> _keyMap = new Dictionary<Keys, int>();

	private struct VertexPositionColorTexture
	{
		public Vector3 Position;
		public Color Color;
		public Vector2 TextureCoordinate;
	}

	public ImGuiRenderer(Game game)
	{
		_game = game;
		_graphicsDevice = game.GraphicsDevice;

		_loadedTextures = new Dictionary<IntPtr, Texture2D>();
		_textureId = 1;

		ImGui.CreateContext();
		var io = ImGui.GetIO();

		// Встановлюємо розмір дисплея
		io.DisplaySize = new System.Numerics.Vector2(
			_graphicsDevice.Viewport.Width,
			_graphicsDevice.Viewport.Height
		);

		// Налаштування базових прапорів ImGui
		io.ConfigFlags |= ImGuiConfigFlags.NavEnableKeyboard;
		io.ConfigFlags |= ImGuiConfigFlags.DockingEnable;

		// Створення графічних ресурсів
		_effect = new BasicEffect(_graphicsDevice)
		{
			TextureEnabled = true,
			VertexColorEnabled = true,
			World = Matrix.Identity,
			View = Matrix.Identity,
			Projection = Matrix.Identity
		};

		_rasterizerState = new RasterizerState()
		{
			CullMode = CullMode.None,
			DepthBias = 0,
			FillMode = FillMode.Solid,
			MultiSampleAntiAlias = false,
			ScissorTestEnable = true,
			SlopeScaleDepthBias = 0
		};

		SetupInput();
		CreateDeviceObjects();
	}

	private void SetupInput()
	{
		_keyMap[Keys.Tab] = (int)ImGuiKey.Tab;
		_keyMap[Keys.Left] = (int)ImGuiKey.LeftArrow;
		_keyMap[Keys.Right] = (int)ImGuiKey.RightArrow;
		_keyMap[Keys.Up] = (int)ImGuiKey.UpArrow;
		_keyMap[Keys.Down] = (int)ImGuiKey.DownArrow;
		_keyMap[Keys.PageUp] = (int)ImGuiKey.PageUp;
		_keyMap[Keys.PageDown] = (int)ImGuiKey.PageDown;
		_keyMap[Keys.Home] = (int)ImGuiKey.Home;
		_keyMap[Keys.End] = (int)ImGuiKey.End;
		_keyMap[Keys.Delete] = (int)ImGuiKey.Delete;
		_keyMap[Keys.Back] = (int)ImGuiKey.Backspace;
		_keyMap[Keys.Enter] = (int)ImGuiKey.Enter;
		_keyMap[Keys.Escape] = (int)ImGuiKey.Escape;
		_keyMap[Keys.A] = (int)ImGuiKey.A;
		_keyMap[Keys.C] = (int)ImGuiKey.C;
		_keyMap[Keys.V] = (int)ImGuiKey.V;
		_keyMap[Keys.X] = (int)ImGuiKey.X;
		_keyMap[Keys.Y] = (int)ImGuiKey.Y;
		_keyMap[Keys.Z] = (int)ImGuiKey.Z;
	}

	private unsafe void CreateDeviceObjects()
	{
		var io = ImGui.GetIO();

		// Створення текстури шрифту
		ImFontAtlasPtr fontAtlas = io.Fonts;
		fontAtlas.GetTexDataAsRGBA32(out IntPtr pixelData, out int width, out int height, out int bytesPerPixel);

		var texData = new byte[width * height * bytesPerPixel];
		Marshal.Copy(pixelData, texData, 0, texData.Length);

		_fontTexture = new Texture2D(_graphicsDevice, width, height, false, SurfaceFormat.Color);
		_fontTexture.SetData(texData);

		_fontTextureId = RegisterTexture(_fontTexture);
		io.Fonts.SetTexID(_fontTextureId);
		io.Fonts.ClearTexData();
	}

	private IntPtr RegisterTexture(Texture2D texture)
	{
		var id = new IntPtr(_textureId++);
		_loadedTextures.Add(id, texture);
		return id;
	}

	public void UnregisterTexture(IntPtr textureId)
	{
		_loadedTextures.Remove(textureId);
	}

	private void UpdateInput()
	{
		var io = ImGui.GetIO();

		var mouse = Mouse.GetState();
		var keyboard = Keyboard.GetState();

		// Оновлення позиції миші
		io.AddMousePosEvent(mouse.X, mouse.Y);

		// Оновлення кнопок миші
		io.AddMouseButtonEvent(0, mouse.LeftButton == ButtonState.Pressed);
		io.AddMouseButtonEvent(1, mouse.RightButton == ButtonState.Pressed);
		io.AddMouseButtonEvent(2, mouse.MiddleButton == ButtonState.Pressed);

		// Оновлення колеса миші
		var scrollDelta = mouse.ScrollWheelValue - _scrollWheelValue;
		io.AddMouseWheelEvent(0, scrollDelta > 0 ? 1 : scrollDelta < 0 ? -1 : 0);
		_scrollWheelValue = mouse.ScrollWheelValue;

		// Оновлення клавіш
		foreach (var key in _keyMap)
		{
			io.AddKeyEvent((ImGuiKey)key.Value, keyboard.IsKeyDown(key.Key));
		}

		// Оновлення модифікаторів
		io.AddKeyEvent(ImGuiKey.ModCtrl, keyboard.IsKeyDown(Keys.LeftControl) || keyboard.IsKeyDown(Keys.RightControl));
		io.AddKeyEvent(ImGuiKey.ModShift, keyboard.IsKeyDown(Keys.LeftShift) || keyboard.IsKeyDown(Keys.RightShift));
		io.AddKeyEvent(ImGuiKey.ModAlt, keyboard.IsKeyDown(Keys.LeftAlt) || keyboard.IsKeyDown(Keys.RightAlt));

		// Додавання символів (для текстового вводу)
		foreach (var c in keyboard.GetPressedKeys())
		{
			if (c >= Keys.A && c <= Keys.Z)
			{
				io.AddInputCharacter((uint)c);
			}
		}
	}

	public void BeginLayout(GameTime gameTime)
	{
		var io = ImGui.GetIO();

		io.DisplaySize = new System.Numerics.Vector2(
			_graphicsDevice.Viewport.Width,
			_graphicsDevice.Viewport.Height
		);
		io.DeltaTime = (float)gameTime.ElapsedGameTime.TotalSeconds;

		UpdateInput();

		ImGui.NewFrame();
	}

	public void EndLayout()
	{
		ImGui.Render();
		unsafe { RenderDrawData(ImGui.GetDrawData()); }
	}

	public void Dispose()
	{
		_effect?.Dispose();
		_rasterizerState?.Dispose();
		_vertexBuffer?.Dispose();
		_indexBuffer?.Dispose();
		_fontTexture?.Dispose();

		foreach (var texture in _loadedTextures.Values)
		{
			texture.Dispose();
		}

		ImGui.DestroyContext();
	}

	private unsafe void RenderDrawData(ImDrawDataPtr drawData)
	{
		if (drawData.CmdListsCount == 0) return;

		// Налаштування рендерингу
		var lastViewport = _graphicsDevice.Viewport;
		var lastScissorBox = _graphicsDevice.ScissorRectangle;

		_graphicsDevice.BlendState = BlendState.NonPremultiplied;
		_graphicsDevice.RasterizerState = _rasterizerState;
		_graphicsDevice.DepthStencilState = DepthStencilState.None;

		// Налаштування матриці проекції
		var projection = Matrix.CreateOrthographicOffCenter(
			0f, _graphicsDevice.Viewport.Width,
			_graphicsDevice.Viewport.Height, 0f,
			-1f, 1f);
		_effect.Projection = projection;

		drawData.ScaleClipRects(ImGui.GetIO().DisplayFramebufferScale);

		// Рендеринг команд
		for (int n = 0; n < drawData.CmdListsCount; n++)
		{
			ImDrawListPtr cmdList = drawData.CmdLists[n];

			// Завантаження вершин та індексів
			int vertexBufferSize = cmdList.VtxBuffer.Size * sizeof(ImDrawVert);
			if (_vertexBuffer == null || _vertexBufferSize < vertexBufferSize)
			{
				_vertexBuffer?.Dispose();
				_vertexBufferSize = vertexBufferSize + 5000;
				_vertexBuffer = new VertexBuffer(
					_graphicsDevice,
					ImGuiVertex.VertexDeclaration,
					_vertexBufferSize / sizeof(ImDrawVert),
					BufferUsage.WriteOnly
				);
			}

			int indexBufferSize = cmdList.IdxBuffer.Size * sizeof(ushort);
			if (_indexBuffer == null || _indexBufferSize < indexBufferSize)
			{
				_indexBuffer?.Dispose();
				_indexBufferSize = indexBufferSize + 10000;
				_indexBuffer = new IndexBuffer(
					_graphicsDevice,
					IndexElementSize.SixteenBits,
					_indexBufferSize / sizeof(ushort),
					BufferUsage.WriteOnly
				);
			}

			// Копіювання даних вершин
			var vertexData = new ImGuiVertex[cmdList.VtxBuffer.Size];
			for (int i = 0; i < cmdList.VtxBuffer.Size; i++)
			{
				var vertex = cmdList.VtxBuffer[i];
				vertexData[i] = new ImGuiVertex
				{
					Position = new Vector2(vertex.pos.X, vertex.pos.Y),
					UV = new Vector2(vertex.uv.X, vertex.uv.Y),
					Color = new Color(
						(byte)(vertex.col >> 0),
						(byte)(vertex.col >> 8),
						(byte)(vertex.col >> 16),
						(byte)(vertex.col >> 24))
				};
			}
			_vertexBuffer.SetData(vertexData);

			// Копіювання даних індексів
			var indexData = new ushort[cmdList.IdxBuffer.Size];
			for (int i = 0; i < cmdList.IdxBuffer.Size; i++)
			{
				indexData[i] = cmdList.IdxBuffer[i];
			}
			_indexBuffer.SetData(indexData);

			_graphicsDevice.SetVertexBuffer(_vertexBuffer);
			_graphicsDevice.Indices = _indexBuffer;

			int indexLocation = 0;

			for (int cmdi = 0; cmdi < cmdList.CmdBuffer.Size; cmdi++)
			{
				ImDrawCmdPtr pcmd = cmdList.CmdBuffer[cmdi];

				var scissorRect = new Rectangle(
					(int)pcmd.ClipRect.X,
					(int)pcmd.ClipRect.Y,
					(int)(pcmd.ClipRect.Z - pcmd.ClipRect.X),
					(int)(pcmd.ClipRect.W - pcmd.ClipRect.Y));
				_graphicsDevice.ScissorRectangle = scissorRect;

				if (pcmd.TextureId != IntPtr.Zero)
				{
					_effect.Texture = _loadedTextures[pcmd.TextureId];
				}

				foreach (EffectPass pass in _effect.CurrentTechnique.Passes)
				{
					pass.Apply();
					_graphicsDevice.DrawIndexedPrimitives(
						PrimitiveType.TriangleList,
						0,                          // vertexStart
						indexLocation,              // startIndex
						(int)pcmd.ElemCount / 3    // primitiveCount
					);
				}

				indexLocation += (int)pcmd.ElemCount;
			}
		}

		// Відновлення стану графічного пристрою
		_graphicsDevice.Viewport = lastViewport;
		_graphicsDevice.ScissorRectangle = lastScissorBox;
	}

	[StructLayout(LayoutKind.Sequential)]
	private struct ImGuiVertex : IVertexType
	{
		public Vector2 Position;
		public Vector2 UV;
		public Color Color;

		public static readonly VertexDeclaration VertexDeclaration = new VertexDeclaration(
			new VertexElement(0, VertexElementFormat.Vector2, VertexElementUsage.Position, 0),
			new VertexElement(8, VertexElementFormat.Vector2, VertexElementUsage.TextureCoordinate, 0),
			new VertexElement(16, VertexElementFormat.Color, VertexElementUsage.Color, 0)
		);

		VertexDeclaration IVertexType.VertexDeclaration => VertexDeclaration;
	}
}