using System.Collections.Generic;
using UnityEngine;


[ExecuteInEditMode]
[RequireComponent(typeof(MeshRenderer))]
[RequireComponent(typeof(MeshFilter))]
public class CinderMedusae : MonoBehaviour {

	public float HeadR = 130;
	
	public float Radius = 1;
	public float Length = 1;

	public int NumTentacles = 4;
	public int NumSpikeWaves = 6;
	public float SpikeWaveAmplitude = 0.175f;
	public float RvecMult = 0.75f;
	
	public float SmoothAngleRange = 60f;
	public float HeadClampAngle = 120f;

	public int TentacleSegments = 20;
	public float TentacleRadius = 20f;
	public float TentacleLength = 250f;
	public int TentacleSides = 4;
	public int TentacleYOffset = 1;
	
	public float TentacleVariationFrequency = 4;
	public float TentacleVariationAmplitude = 1;
	public float TentacleVariationOffset = 0.5f;
		
	public int Nsides = 20;
	public int Nsegments = 10;

	public bool animate;

	public float AnimationFrequency = 1f;
	public float AnimationSpeed = 4f;
	public float AnimationAmount = 3f;
	public float TentacleAnimationMultiplier = 0.5f;

	private Mesh mesh;
	private MeshFilter meshFilter;
	private int bodyVertexCount;
	
	private List<Vector3> vertices;
	private List<Vector3> normals;
	

	void Start () {
		meshFilter = gameObject.GetComponent<MeshFilter>();
		vertices.Clear();
		normals.Clear();
		BuildMesh();
	}
	
	void Update() {
		
		if (vertices == null)
			vertices = new List<Vector3>();			
		if (normals == null)
			normals = new List<Vector3>();

		if (vertices.Count > 0 && animate)
		{
			AnimateMesh();			
		}
		
	}
	
	private static float Map(float s, float a1, float a2, float b1, float b2)
	{
		return b1 + (s - a1) * (b2 - b1) / (a2 - a1);
	}
	
	private Vector3 EvalHeadVertex(float theta, float phi) {
		
		Vector3 rvec = new Vector3(HeadR * Radius, HeadR * Length, HeadR * Radius);
		
		Vector3 v = new Vector3();		
		float smoothAngleRange = Mathf.Deg2Rad * SmoothAngleRange; 
		float headClampAngle = Mathf.Deg2Rad * HeadClampAngle;
		
		if (theta < headClampAngle) {
			v.x = rvec.x * Mathf.Sin(theta) * Mathf.Sin(phi);
			v.y = rvec.y * Mathf.Cos(theta);
			v.z = rvec.z * Mathf.Sin(theta) * Mathf.Cos(phi);
		}
		else
		{
			if (theta >= headClampAngle && theta <= headClampAngle + smoothAngleRange) {
				float curveAngle = Map(theta, headClampAngle, headClampAngle + smoothAngleRange, 0, Mathf.PI);				
				v.x = rvec.x * Mathf.Sin(theta) * Mathf.Sin(phi);
				v.y = rvec.y * Mathf.Cos(headClampAngle) - Mathf.Sin(curveAngle) * HeadR * 0.1f;
				v.z = rvec.z * Mathf.Sin(theta) * Mathf.Cos(phi);
			}
			else
			{
				theta = Map(theta, headClampAngle + smoothAngleRange, Mathf.PI, Mathf.PI/2, 0); //headClampAngle
				rvec *= RvecMult;
				v.x = rvec.x * Mathf.Sin(theta) * Mathf.Sin(phi);
				v.y = rvec.y * Mathf.Cos(theta) - rvec.y * 0.5f;
				v.z = rvec.z * Mathf.Sin(theta) * Mathf.Cos(phi);
			}
		}

		v.x *= 1f + SpikeWaveAmplitude * Mathf.Cos(phi * NumSpikeWaves) * theta/Mathf.PI;
		v.z *= 1f + SpikeWaveAmplitude * Mathf.Cos(phi * NumSpikeWaves) * theta/Mathf.PI;	
		
		return v;
	}
	
	private float EvalTentacleSegmentRadius(float t) {  
		if (t < 0.25f) {
			return TentacleRadius;
		}
		else if (t < 0.75f)
		{
			return TentacleRadius * 0.5f;
		}
		else
		{
			return TentacleRadius * (1.0f - (t - 0.75f) / 0.25f) * 0.5f;
		}
	} 

	
	TentacleSegment EvalTentacleSegmentPosition(Vector3 startPos, Vector3 forward, float t, float tentacleLength) {
		var joint = new TentacleSegment();  
		Vector3 fwd = new Vector3(forward.x, forward.y, forward.z);
		fwd *= 20f;
		joint.Forward = forward;
		joint.Pos = startPos;
		joint.PrevPos = joint.Pos - fwd;
		joint.Pos = joint.PrevPos;
		joint.Left = Vector3.Cross(new Vector3(0, 1, 0), joint.Forward);
		joint.Left.Normalize();
		joint.Up = Vector3.Cross(joint.Forward, joint.Left);
		joint.Up.Normalize();
		Vector3 gravity = new Vector3(0, -0.5f, 0);		
		joint.Radius = TentacleRadius;
		if (t < 0.000001f) {			
			return joint;
		}    
		for(float f=0; f<1; f+=0.02f) {	
			joint.Pos = joint.Pos + joint.Forward * tentacleLength * 0.02f;
			joint.Forward = joint.Pos - joint.PrevPos;		
			joint.Forward = joint.Forward + gravity;  
			joint.Forward.Normalize();
			joint.Left = Vector3.Cross(joint.Up, joint.Forward);
			joint.Left.Normalize();
			joint.Up = Vector3.Cross(joint.Forward, joint.Left);
			joint.Up.Normalize();		
			joint.Radius = EvalTentacleSegmentRadius(f);		
			if (f >= t) break;
			joint.PrevPos = joint.Pos; 		
		}		
		return joint;
	}   
	
	private Mesh DrawTentacle(Vector3 startPos, Vector3 forward, float tentacleLength) {
		
		var tentacleVerts = new List<Vector3>();
		var tentacleTris = new List<int>();
	  
		int idx = 0;
   
		for(int s=0; s < TentacleSegments; s++) {
			var joint = EvalTentacleSegmentPosition(startPos, forward, s/(TentacleSegments-1.0f), tentacleLength);				
		 		
			for(int j=0; j <= TentacleSides; j++) {
				float rd = 2 * Mathf.PI * j / TentacleSides;
				//Vec3f pos = joint.position + cosf(rad) * joint.left * joint.r.x + sinf(rad) * joint.up * joint.r.y;
				Vector3 pos = new Vector3(joint.Pos.x, joint.Pos.y, joint.Pos.z);
				Vector3 left = new Vector3(joint.Left.x, joint.Left.y, joint.Left.z);			
				Vector3 up = new Vector3(joint.Up.x, joint.Up.y, joint.Up.z);			
				left *= joint.Radius * Mathf.Cos(rd);			
				up *= joint.Radius * Mathf.Sin(rd);			
				pos += left;  
				pos += up;
			
				//sine wave, disabled
				//Vector3 wave = new Vector3(forward.x, forward.y, forward.z);  						
				//wave.mult(2 * sin(pos.y*PI*5 + animTime));
				//pos.add(wave);	  
			
				tentacleVerts.Add(pos);
				if (s > 0 && j < TentacleSides) {     
					tentacleTris.Add(idx + j - TentacleSides - 1);
					tentacleTris.Add(idx + j + 1);
					tentacleTris.Add(idx + j);
				
					tentacleTris.Add(idx + j - TentacleSides - 1);
					tentacleTris.Add(idx + j - TentacleSides);
					tentacleTris.Add(idx + j + 1);
				}	    								
			}  
			idx += TentacleSides + 1; 
		}

		var tmesh = new Mesh
		{
			vertices = tentacleVerts.ToArray(),
			triangles = tentacleTris.ToArray()
		};
		tmesh.RecalculateNormals();
		return tmesh;
	}


	public void AnimateMesh()
	{
		Debug.Log(bodyVertexCount);
		for (int index = 0; index < vertices.Count; index++)
		{
			if (index < bodyVertexCount)
			{
				vertices[index] += Mathf.Sin(vertices[index].y / HeadR * Mathf.PI * AnimationFrequency + Time.time * AnimationSpeed) * (normals[index] * AnimationAmount);				
			}
			else
			{
				vertices[index] += Mathf.Sin(vertices[index].y / HeadR * Mathf.PI * AnimationFrequency + Time.time * AnimationSpeed) * (normals[index] * AnimationAmount) * TentacleAnimationMultiplier;				
			}
		}
		mesh.vertices = vertices.ToArray();
		//mesh.RecalculateNormals();
		//mesh.RecalculateTangents();
		//mesh.RecalculateBounds();
		
		if (meshFilter != null)
		{
			if (Application.isPlaying)
			{
				meshFilter.mesh = mesh;
			}
			else
			{
				meshFilter.sharedMesh = mesh;				
			}
		}
	}

	public void BuildMesh()
	{
		mesh = new Mesh {name = "CinderMedusae"};
		CombineInstance[] combine = new CombineInstance[NumTentacles + 1];

		if (vertices == null)
		{
			vertices = new List<Vector3>();
		}
		else
		{
			vertices.Clear();			
		}

		if (normals == null)
		{
			normals = new List<Vector3>();
		}
		else
		{
			normals.Clear();
		}
		
		float dtheta = Mathf.PI / Nsegments;
		float dphi = 2 * Mathf.PI / Nsides;

		var triangles = new List<int>();

		float phi;
		float theta = 0;

		int tentacleIndex = 0;

		for (int segment = 0; segment <= Nsegments; ++segment)
		{
			theta += dtheta;
			phi = 0;

			for (int side = 0; side <= Nsides; ++side)
			{
				phi += dphi;

				Vector3 v1 = EvalHeadVertex(theta, phi);
				Vector3 v2 = EvalHeadVertex(theta + dtheta, phi);
				Vector3 v3 = EvalHeadVertex(theta, phi + dphi);

				Vector3 a = v2 - v1;
				Vector3 b = v3 - v1;
				Vector3 normal = Vector3.Cross(a, b);
				normal.Normalize();

				if (normal.magnitude < 0.000000001)
				{
					normal = new Vector3(0, 1, 0);
				}
				
				vertices.Add(v1);
				normals.Add(normal);
				
				if (side % ((float)Nsides/NumTentacles) < 1 && segment == Nsegments/2 + TentacleYOffset)
				{
					var ttransform = new Matrix4x4();
					ttransform.SetTRS(v1, Quaternion.LookRotation(normal), Vector3.one);
					float tentacleLengthMultiplier = (Mathf.Sin(phi * TentacleVariationFrequency) + TentacleVariationOffset) * TentacleVariationAmplitude;
					var tmesh = DrawTentacle(Vector3.zero, Vector3.forward, TentacleLength * tentacleLengthMultiplier);
					combine[tentacleIndex + 0].mesh = tmesh;
					combine[tentacleIndex + 0].transform = ttransform;
					tentacleIndex++;
				}

				if (segment == Nsegments) continue;
				if (side == Nsides) continue;

				triangles.Add(segment * (Nsides + 1) + side);
				triangles.Add((segment + 1) * (Nsides + 1) + side);
				triangles.Add((segment + 1) * (Nsides + 1) + side + 1);

				triangles.Add(segment * (Nsides + 1) + side);
				triangles.Add((segment + 1) * (Nsides + 1) + side + 1);
				triangles.Add(segment * (Nsides + 1) + side + 1);

			}
		}
		
		var bodyMesh = new Mesh
		{
			vertices = vertices.ToArray(),
			triangles = triangles.ToArray(),
			normals = normals.ToArray()
		};

		bodyVertexCount = vertices.Count;
		
		//mesh.uv = meshUV;
		var btransform = new Matrix4x4();
		btransform.SetTRS(Vector3.zero, Quaternion.identity, Vector3.one);
		combine[0].mesh = bodyMesh;
		combine[0].transform = btransform;

		if (meshFilter != null)
		{
			mesh.CombineMeshes(combine);
			mesh.RecalculateNormals();
			mesh.RecalculateTangents();
			mesh.RecalculateBounds();

			if (Application.isPlaying)
			{
				meshFilter.mesh = new Mesh();
				meshFilter.mesh = mesh;
			}
			else
			{
				meshFilter.sharedMesh = new Mesh();
				meshFilter.sharedMesh = mesh;
			}
			
			if (animate)
			{
				mesh.GetVertices(vertices);
				mesh.GetNormals(normals);
			}
		}
	}

	public void OnValidate()
	{
		BuildMesh();
//		var currentState = this;
//		if (previousState != currentState)
//		{
//			BuildMesh();
//			previousState = currentState;
//		}
	}

//	void OnDrawGizmos()
//	{
//		float GizmoRadius = 6f;
//
//		var offset = this.transform;
//
//		Gizmos.color = Color.white;
//		if (mesh.vertices != null)
//		{
//			for (int i = 0; i < mesh.vertices.Length; i++)
//			{
//				Vector3 vert = mesh.vertices[i];
//				Vector3 pos = offset.TransformPoint(vert);
//				Gizmos.DrawWireSphere(pos, GizmoRadius);
//			}
//		}
//
//	}
	
}

internal class TentacleSegment : Object
{
	public Vector3 Forward;
	public Vector3 Pos;	
	public Vector3 PrevPos;
	public Vector3 Left;
	public Vector3 Up;
	public float Radius;
}
