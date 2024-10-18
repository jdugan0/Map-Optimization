using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

public partial class Renderer : Sprite2D
{
	// Called when the node enters the scene tree for the first time.
	[Export] int pixelHeight;
	[Export] int pixelWidth;
	[Export] int pts;
	ImageTexture texture = new ImageTexture();
	Gradient gradient = new Gradient();
	public List<Pixel> pixels = new List<Pixel>();
	Dictionary<Vector2I, Pixel> pixelGrid = new Dictionary<Vector2I, Pixel>();
	[Export] public float rand;
	Vector2I[] neighborOffsets = {
			new Vector2I(-1, -1), new Vector2I(0, -1), new Vector2I(1, -1),
			new Vector2I(-1, 0), /* current pixel */    new Vector2I(1, 0),
			new Vector2I(-1, 1), new Vector2I(0, 1), new Vector2I(1, 1)
		};
	public override void _Ready()
	{
		List<Vector2> points = new List<Vector2>();
		for (int i = 0; i < pts; i++)
		{
			points.Add(new Vector2(GD.Randf() * pixelWidth, GD.Randf() * pixelHeight));
		}

		gradient.RemovePoint(1);
		gradient.AddPoint(0, Colors.Red);

		gradient.AddPoint(pts - 1, Colors.Blue);

		for (int x = 0; x < pixelWidth; x++)
		{
			for (int y = 0; y < pixelHeight; y++)
			{
				Vector2 pos = new Vector2(x, y);

				float bestDist = float.MaxValue;
				int id = -1;
				foreach (Vector2 point in points)
				{
					if (point.DistanceSquaredTo(pos) < bestDist)
					{
						bestDist = point.DistanceSquaredTo(pos);
						id = points.IndexOf(point);
					}
				}
				float prop = GD.Randf();
				int pop = GD.RandRange(10, 265);
				Pixel p = new Pixel(new Vector2I(x, y), prop, pop, id);

				foreach (Vector2I offset in neighborOffsets)
				{
					Vector2I neighborPos = new Vector2I(x + offset.X, y + offset.Y);
					if (pixelGrid.ContainsKey(neighborPos))
					{
						Pixel neighbor = pixelGrid[neighborPos];
						p.boundary.Add(neighbor);
						neighbor.boundary.Add(p);
					}
				}
				pixelGrid.Add(p.pos, p);
				pixels.Add(p);
			}
		}
		// GD.Print(pixels.Count);
		Render();
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
		Update();
		Render();
	}

	public void Update()
	{
		float initScore = ScoreCOM();
		// GD.Print("InitScore: " + initScore);
		Pixel[] boundary = findBoundary().ToArray();
		Pixel p = boundary[GD.RandRange(0, boundary.Length - 1)];
		int initalDistrict = p.district;
		p.district = p.boundDistrict;
		float final = ScoreCOM();
		if (final >= initScore)
		{
			if (GD.Randf() > rand)
			{
				p.district = initalDistrict;
			}
		}
		GD.Print(final);
	}
	public float ScoreSize(){
		float[] count = new float[pts];
		foreach (Pixel p in pixels){
			count[p.district]++;
		}
		float biggest = float.MinValue;
		float smallest = float.MaxValue;
		foreach (float c in count){
			if (c > biggest){
				biggest = c;
			}
			else if (c < smallest) { 
				smallest = c;
			}
		}
		return biggest - smallest;
	}
	public float ScoreCOM()
	{
		float score = 0;

		
		float[] sumX = new float[pts];
		float[] sumY = new float[pts];
		int[] count = new int[pts];

		
		foreach (var pixel in pixels)
		{
			int district = pixel.district;
			sumX[district] += pixel.pos.X;
			sumY[district] += pixel.pos.Y;
			count[district]++;
		}

		
		Parallel.For(0, pts, district =>
		{
			if (count[district] == 0) return;  // Skip empty districts

			
			Vector2 com = new Vector2(sumX[district] / count[district], sumY[district] / count[district]);

			float districtSum = 0;
			foreach (var pixel in pixels)
			{
				if (pixel.district == district)
				{
					districtSum += com.DistanceTo(pixel.pos);
				}
			}

			
			lock (this)
			{
				score += districtSum / count[district];
			}
		});

		return score;

	}
	public void Render()
	{
		Image image = Image.Create(pixelWidth, pixelHeight, false, Image.Format.Rgba8);

		foreach (Pixel pixel in pixels)
		{
			// GD.Print(pixel.boundary.Count);
			// GD.Print(pixel.district);
			image.SetPixel(pixel.pos.X, pixel.pos.Y, gradient.Sample(pixel.district));
			// image.SetPixel(pixel.pos.X, pixel.pos.Y, gradient.Sample(pixel.district));

		}

		texture.SetImage(image);
		Texture = texture;
	}
	public List<Pixel> findBoundary()
	{
		List<Pixel> boundary = new List<Pixel>();
		Parallel.ForEach(pixels, p =>
		{

			foreach (Pixel p1 in p.boundary)
			{
				if (p.district != p1.district)
				{
					lock (boundary)
					{
						boundary.Add(p);
						p.boundDistrict = p1.district;
					}
					break;
				}
			}
		});
		return boundary;
	}
}
