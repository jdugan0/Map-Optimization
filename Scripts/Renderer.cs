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
	ulong averageUpdate = 0;
	ulong averageRender = 0;
	ulong count;
	[Export] public float rand;
	public float[] districtCounts;
	Vector2I[] neighborOffsets = {
			new Vector2I(-1, -1), new Vector2I(0, -1), new Vector2I(1, -1),
			new Vector2I(-1, 0), /* current pixel */    new Vector2I(1, 0),
			new Vector2I(-1, 1), new Vector2I(0, 1), new Vector2I(1, 1)
		};
	public override void _Ready()
	{
		districtCounts = new float[pts];
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
		UpdateCounts();
		Render();
	}
	public float ScoreCount()
	{
		float min = float.MaxValue;
		float max = float.MinValue;
		foreach (float f in districtCounts)
		{
			if (f < min)
			{
				min = f;
			}
			if (f > max)
			{
				max = f;
			}
		}
		return max - min;
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
		ulong time1 = Time.GetTicksUsec();
		Update();
		ulong time2 = Time.GetTicksUsec();
		Render();
		ulong time3 = Time.GetTicksUsec();
		averageUpdate += (time2 - time1);
		averageRender += (time3 - time2);
		count++;
		GD.Print("Update Time: " + (time2 - time1));
		GD.Print("Render Time: " + (time3 - time2));
	}
	private int DFS(Pixel p, Pixel init)
	{
		HashSet<Pixel> visited = new HashSet<Pixel>();
		HashSet<Pixel> total = new HashSet<Pixel>();
		Stack<Pixel> stack = new Stack<Pixel>();
		stack.Push(p);
		visited.Add(p);
		total.Add(p);


		while (stack.Count > 0)
		{
			Pixel current = stack.Pop();
			foreach (Pixel neighbor in current.boundary)
			{
				if (neighbor.district == p.district && !visited.Contains(neighbor))
				{
					visited.Add(neighbor);
					if (init.boundary.Contains(neighbor))
					{
						stack.Push(neighbor);
						total.Add(neighbor);

					}
				}
			}
		}
		return total.Count;
	}
	public float CalculateScore()
	{
		return ScoreCount() + ScoreCOM() * 5 + ScoreBorder() * 8;
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
	public void Update()
	{
		GD.Print(districtCounts[0]);
		float initScore = CalculateScore();
		// GD.Print("InitScore: " + initScore);
		Pixel[] boundary = findBoundary().ToArray();
		while (true)
		{
			bool broken = false;


			Pixel p = boundary[GD.RandRange(0, boundary.Length - 1)];
			int initalDistrict = p.district;
			ChangeDistrict(p);

			//check if connection is broken and set variable: 
			Pixel start = null;
			int count = 0;
			foreach (Pixel p1 in p.boundary)
			{
				if (p1.district == initalDistrict)
				{
					count++;
					start = p1;
				}
			}
			if (start != null)
			{
				if (DFS(start, p) < count)
				{
					broken = true;
				}
			}

			if (!broken)
			{
				float final = CalculateScore();
				if (final >= initScore)
				{
					if (GD.Randf() > rand)
					{
						RevertDistrict(p, initalDistrict);
					}
					else
					{
						GD.Print(final);
						break;
					}
				}
				else
				{
					GD.Print(final);
					break;
				}
			}
			else
			{
				RevertDistrict(p, initalDistrict);
			}
		}
		// GD.Print(final);
	}
	public float ScoreBorder()
	{
		float score = 0;
		foreach (Pixel p in pixels)
		{
			foreach (Pixel p1 in p.boundary)
			{
				if (p1.district != p.district)
				{
					score += 1;
				}
			}
		}
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
	public void UpdateCounts()
	{
		foreach (Pixel p in pixels)
		{
			districtCounts[p.district]++;
		}
		GD.Print(districtCounts);
	}
	public void ChangeDistrict(Pixel p)
	{
		districtCounts[p.district]--;
		p.district = p.boundDistrict;
		districtCounts[p.district]++;
	}
	public void RevertDistrict(Pixel p, int district)
	{
		districtCounts[p.district]--;
		districtCounts[district]++;
		p.district = district;
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
