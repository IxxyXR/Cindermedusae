using System.Collections.Generic;
using UnityEngine;


[ExecuteInEditMode]
[RequireComponent(typeof(MeshRenderer))]
[RequireComponent(typeof(MeshFilter))]
public class Cindermedusae : MonoBehaviour
{

	public int Nsides = 64;
	public int Nsegments = 32;
	public float Radius = 1;
	public float Length = 1;
	
	private float thetaStep; 
	private float wrapAngle;
	private float smoothRange;

	private Mesh mesh;
	private MeshFilter meshFilter;
	//private Cindermedusae previousState;
	
	void Start () {
		
		meshFilter = gameObject.GetComponent<MeshFilter>();
		thetaStep = Mathf.PI / Nsegments;
		wrapAngle = Mathf.PI/2 + thetaStep * 3;
		smoothRange = thetaStep * 4;
		
	}
	
	void Update() {
		if (Input.GetKeyDown("p")) {
			gameObject.GetComponent<RotateObject>().Pause();
		} else if (Input.GetKeyDown("r")) {
			gameObject.GetComponent<RotateObject>().Randomize();
		}	
	}

	public static float Map(float s, float a1, float a2, float b1, float b2)
	{
		return b1 + (s-a1)*(b2-b1)/(a2-a1);
	}
	

	public Vector3 EvalVertex(float theta) {
			
		//we subtract Mathf.PI/2 aka 90' so the first point is on top instead on the right   
		var p = new Vector3();
		if (theta < wrapAngle) {
			p.x = Radius * Mathf.Cos(theta - Mathf.PI/2);    
			p.y = Radius * Mathf.Sin(theta - Mathf.PI/2); 
		}                                      
		else {                                
			if (theta <= wrapAngle + smoothRange + 0.01) {   
				float t = Map(theta, wrapAngle, wrapAngle + smoothRange, 0, Mathf.PI/2);
				p.x = Radius * Mathf.Cos(theta - Mathf.PI/2);                     
				p.y = Radius * Mathf.Sin(wrapAngle - Mathf.PI/2) + 20 * Mathf.Sin(t);
			}   
			else {   
				float t = Map(theta, wrapAngle + smoothRange, Mathf.PI, Mathf.PI/2, 0);
				p.x = 0.75f * Radius * Mathf.Cos(t - Mathf.PI/2);              
				p.y = 0.75f * Radius * Mathf.Sin(t - Mathf.PI/2) + Radius * 0.5f;
			}
		}
		 
		return p;
	}  
		
	public Vector2 EvalNormal(float theta) {   
		var p1 = EvalVertex(theta);
		var p2 = EvalVertex(theta - thetaStep/2);	
		var n = new Vector2(); 
			
		n.x = -(p2.y - p1.y);
		n.y = p2.x - p1.x;  
			
		n = Normalize(n, 10);
		return n;
	}      
		
	public static Vector3 Normalize(Vector3 v, float newLen) {
			
		newLen = Mathf.Max(newLen, 1f);
			
		float len = Mathf.Sqrt(v.x * v.x + v.y * v.y);
			
		if (len == 0f) {
			return v;
		}            
		v.x /= len;
		v.y /= len;   
			
		v.x *= newLen;
		v.y *= newLen;
		return v;
	}
		
	public Vector2[] Segment()
	{
		var points = new Vector2[Nsegments + 1];		
				
		for(int i=0; i<Nsegments+1; i++) { 	
			var p1 = EvalVertex(i * thetaStep);
			//var n1 = EvalNormal(i * thetaStep);
			points[i] = new Vector2(p1.x, p1.y);
			i++;
		}

		return points;

	}  
	
	public void BuildMesh()
	{
		mesh = new Mesh();
		mesh.name = "CinderMedusae";

		int numVertices = (Nsides + 1) * (Nsegments + 1);
		int numUVs = numVertices;
		int numTris = Nsides * Nsegments * 2;
		var meshVertices = new Vector3[numVertices];
		var meshTriangles = new int[numTris * 3];
		Vector2[] meshUV = new Vector2[ numUVs ];
		
		float x, y, z;

		float colAngleStep = 2 * Mathf.PI / Nsegments;
		
		var segment = Segment();
		
		for (int row = 0; row < Nsegments + 1; row++)		
		{
			for (int column = 0; column < Nsides + 1; column++)
			{
				float phi = column * colAngleStep;
				//float theta = row * rowAngleStep;
				
				x = segment[row].x;
				y = Mathf.Sin(phi) * Radius;
				z = segment[row].y;
				
				meshVertices[row * Nsides + 1 + column] = new Vector3(x, y, z);
				
				meshUV[row * Nsides + 1 + column] = new Vector2(column * 1.0f/Nsides, row * 1.0f/Nsegments);
				
				if(row == 0 || column >= Nsides){
					continue;
				}

				int triIndex = (row - 1) * Nsides * 6 + column * 6;

				meshTriangles[triIndex + 0] = row * Nsides + 1 + column;
				meshTriangles[triIndex + 1] = (row - 1) * Nsides + 1 + column;
				meshTriangles[triIndex + 2] = row * Nsides + 1 + column + 1;

				meshTriangles[triIndex + 3] = (row - 1) * Nsides + 1 + column;
				meshTriangles[triIndex + 4] = (row - 1) * Nsides + 1 + column + 1;
				meshTriangles[triIndex + 5] = row * Nsides + 1 + column + 1;
				
			}

		}

		mesh.vertices = meshVertices;
		mesh.triangles = meshTriangles;
		mesh.uv = meshUV;
		
		mesh.RecalculateNormals();
		mesh.RecalculateBounds();
		mesh.RecalculateTangents();
		
		if (meshFilter != null)
		{
			meshFilter.mesh = mesh;
		}
	}

	private void OnValidate()
	{
		BuildMesh();
//		var currentState = this;
//		if (previousState != currentState)
//		{
//			BuildMesh();
//			previousState = currentState;
//		}
	}


	void OnDrawGizmos()
	{
		float GizmoRadius = .02f * Length * Radius;

		var transform = this.transform;

//		Gizmos.color = Color.white;
//		if (mesh.vertices != null)
//		{
//			for (int i = 0; i < mesh.vertices.Length; i++)
//			{
//				Vector3 vert = mesh.vertices[i];
//				Vector3 pos = transform.TransformPoint(vert);
//				Gizmos.DrawWireSphere(pos, GizmoRadius);
//			}
//		}

	}
}
