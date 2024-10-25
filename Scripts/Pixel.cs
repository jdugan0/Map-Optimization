using Godot;
using System;
using System.Collections.Generic;

public partial class Pixel
{
	public Vector2I pos = new Vector2I();
	public Color color = new Color();
	public float voterProp;
	public int pop;
	public int district;
	public int boundDistrict;
	public List<Pixel> boundaryNoDiagonal = new List<Pixel>();
	public List<Pixel> boundary = new List<Pixel>();

	public Pixel(Vector2I pos, float voterProp, int pop, int district){
        this.pos = pos;
		
		this.voterProp = voterProp;

		this.pop = pop;
        this.district = district;

    }
}
