using System;
using System.Collections.Generic;
using Eto.Drawing;
using Eto.Forms;

namespace Eto.Test.Sections.Drawing
{
	public class DirectDrawingSection : Scrollable
	{
		readonly Drawable drawable;
		readonly UITimer timer;
		DirectDrawingRenderer renderer = new DirectDrawingRenderer();

		public DirectDrawingSection ()
		{
			timer = new UITimer {
				Interval = 0.001
			};
			drawable = new Drawable ();
			drawable.BackgroundColor = Colors.Black;
			timer.Elapsed += (sender, e) => {
				if (this.ParentWindow == null) {
					timer.Stop ();
					return;
				}

				try
				{
					using (var graphics = drawable.CreateGraphics())
						renderer.DrawFrame(graphics, drawable.Size);
				}
				catch (NotSupportedException)
				{
					timer.Stop();
					this.BackgroundColor = Colors.Red;
					this.Content = new Label { Text = "This platform does not support direct drawing", TextColor = Colors.White, VerticalAlign = VerticalAlign.Middle, HorizontalAlign = HorizontalAlign.Center };
				}
			};

			var layout = new DynamicLayout (new Padding(10));
			layout.AddSeparateRow (null, UseTexturesAndGradients (), null);
			layout.Add (drawable);
			this.Content = layout;
		}

		Control UseTexturesAndGradients ()
		{
			var control = new CheckBox {
				Text = "Use Textures && Gradients",
				Checked = renderer.UseTexturesAndGradients
			};
			control.CheckedChanged += (sender, e) => {
				renderer.UseTexturesAndGradients = control.Checked ?? false;
				using (var graphics = drawable.CreateGraphics ()) {
					graphics.Clear ();
				}
				lock (renderer.Boxes)
					renderer.Boxes.Clear ();
			};
			return control;
		}

		public override void OnLoadComplete (EventArgs e)
		{
			base.OnLoadComplete (e);
			timer.Start ();
		}

		protected override void Dispose (bool disposing)
		{
			base.Dispose (disposing);
			if (disposing)
				timer.Stop ();
		}
	}

	public class DirectDrawingRenderer
	{
		public readonly List<Box> Boxes = new List<Box>();
		public bool UseTexturesAndGradients { get; set; }
		static readonly Image texture = TestIcons.Textures;

		public class Box
		{
			static readonly Random random = new Random();
			SizeF increment;
			readonly Color color;
			readonly float rotation;
			float angle;
			readonly Action<Graphics> draw;
			readonly Action<Graphics> erase;
			readonly Brush fillBrush;
			RectangleF position;
			IMatrix transform;

			public SizeF Increment { get { return increment; } set { increment = value; } }

			static Color GetRandomColor(Random random)
			{
				return Color.FromArgb(random.Next(byte.MaxValue), random.Next(byte.MaxValue), random.Next(byte.MaxValue));
			}

			public Box(Size canvasSize, bool useTexturesAndGradients)
			{
				var size = new SizeF(random.Next(50) + 50, random.Next(50) + 50);
				var location = new PointF(random.Next(canvasSize.Width - (int)size.Width), random.Next(canvasSize.Height - (int)size.Height));
				position = new RectangleF(location, size);
				increment = new SizeF(random.Next(3) + 1, random.Next(3) + 1);
				if (random.Next(2) == 1)
					increment.Width = -increment.Width;
				if (random.Next(2) == 1)
					increment.Height = -increment.Height;

				angle = random.Next(360);
				rotation = (random.Next(20) - 10f) / 4f;

				var rect = new RectangleF(size);
				color = GetRandomColor(random);
				switch (random.Next(useTexturesAndGradients ? 4 : 2))
				{
					case 0:
						draw = g => g.DrawRectangle(color, rect);
						erase = g => g.DrawRectangle(Colors.Black, rect);
						break;
					case 1:
						draw = g => g.DrawEllipse(color, rect);
						erase = g => g.DrawEllipse(Colors.Black, rect);
						break;
					case 2:
						switch (random.Next(2))
						{
							case 0:
								fillBrush = new LinearGradientBrush(GetRandomColor(random), GetRandomColor(random), PointF.Empty, new PointF(size.Width, size.Height));
								break;
							case 1:
								fillBrush = new TextureBrush(texture)
								{
									Transform = Matrix.FromScale(size / 80)
								};
								break;
						}
						draw = g => g.FillEllipse(fillBrush, rect);
						erase = g => g.FillEllipse(Colors.Black, rect);
						break;
					case 3:
						switch (random.Next(2))
						{
							case 0:
								fillBrush = new LinearGradientBrush(GetRandomColor(random), GetRandomColor(random), PointF.Empty, new PointF(size.Width, size.Height));
								break;
							case 1:
								fillBrush = new TextureBrush(texture)
								{
									Transform = Matrix.FromScale(size / 80)
								};
								break;
						}
						draw = g => g.FillRectangle(fillBrush, rect);
						erase = g => g.FillRectangle(Colors.Black, rect);
						break;
				}
			}

			public void Move(Size bounds)
			{
				position.Offset(increment);
				var center = position.Center;
				if (increment.Width > 0 && center.X >= bounds.Width)
					increment.Width = -increment.Width;
				else if (increment.Width < 0 && center.X < 0)
					increment.Width = -increment.Width;

				if (increment.Height > 0 && center.Y >= bounds.Height)
					increment.Height = -increment.Height;
				else if (increment.Height < 0 && center.Y < 0)
					increment.Height = -increment.Height;
				angle += rotation;

				transform = Matrix.FromTranslation(position.Location);
				transform.RotateAt(angle, position.Width / 2, position.Height / 2);
			}

			public void Erase(Graphics graphics)
			{
				if (transform != null)
				{
					graphics.SaveTransform();
					graphics.MultiplyTransform(transform);
					erase(graphics);
					graphics.RestoreTransform();
				}
			}

			public void Draw(Graphics graphics)
			{
				graphics.SaveTransform();
				graphics.MultiplyTransform(transform);
				draw(graphics);
				graphics.RestoreTransform();
			}
		}

		void InitializeBoxes(Size canvasSize)
		{
			for (int i = 0; i < 20; i++)
				Boxes.Add(new Box(canvasSize, UseTexturesAndGradients));
		}

		public void DrawFrame(Graphics graphics, Size canvasSize)
		{
			lock (Boxes)
			{
				if (Boxes.Count == 0)
					InitializeBoxes(canvasSize);

				var bounds = canvasSize;
				graphics.AntiAlias = false;
				foreach (var box in Boxes)
				{
					box.Erase(graphics);
					box.Move(bounds);
					box.Draw(graphics);
				}
			}
		}
	}
}

