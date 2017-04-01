using UnityEngine;
using System.Collections;
using OpenCVForUnity;
using System;
using System.Collections.Generic;
using System.IO;
using System.Globalization;
using System.Security.Cryptography;

public class PersonInfo
{
	public UnityEngine.Rect r;
	public Vector3 L;
	public Vector3 R;
	public Vector3 U;
	public Vector3 D;

	public PersonInfo (UnityEngine.Rect r0, Vector3 L0, Vector3 R0, Vector3 U0, Vector3  D0)
	{
		r = r0;
		L = L0;
		R = R0;
		U = U0;
		D = D0;
	}
};

static public class Global
{
	static public Camera cam;
	static public GameObject[] personObjects;
	static public int imgOnScrL;
	static public int imgOnScrR;
	static public int totalbackgroundNum = 1719;
	static public string backImgDir = "../../DATA/background/images/";
	static public GameObject light;
	static public Vector3 toScreen (Vector3 v)
	{
		return  cam.WorldToScreenPoint (v);
	}

	static public Vector3 toWorld (Vector3 v)
	{
		return  cam.ScreenToWorldPoint (v);
	}

	static public GameObject getRootObject (GameObject childObject)
	{ 
		if (childObject.transform.parent == null) {    
			return childObject;    
		} else {    
			return getRootObject (childObject.transform.parent.gameObject);    
		}    
	}

	static public PersonInfo getRectFromPerson (GameObject p, GameObject rootNow)
	{
		ArrayList PersonInfo = new ArrayList ();
		GameObject meshObj = p.transform.Find ("mesh").gameObject;
		Vector3 center = p.transform.Find ("mesh").transform.position;
		Mesh colliderMesh = new Mesh();
		meshObj.GetComponent<SkinnedMeshRenderer>().BakeMesh(colliderMesh);
		meshObj.GetComponent<MeshCollider> ().sharedMesh = colliderMesh;


		Vector3 c = toScreen (center);

		float xMin = 100000f;
		float yMin = 100000f;
		float xMax = -100000f;
		float yMax = -100000f;
		Vector3 L = new Vector3 ();
		Vector3 R = new Vector3 ();
		Vector3 U = new Vector3 ();
		Vector3 D = new Vector3 ();

		float searchRangex = 600f;
		float searchRangey = 900f;
		for (float i = c.x - searchRangex; i < c.x + searchRangex; i = i + 5) {
			if(i < imgOnScrL || i > imgOnScrR){
				continue;
			}
			for (float j = c.y - searchRangey; j < c.y + searchRangey; j = j + 5) {
				if(j < 0 || j > Screen.height){
					continue;
				}
				float disMin = 1000000f;
				RaycastHit[] hit = Physics.RaycastAll (cam.ScreenPointToRay (new Vector3 (i, j, 0)));
				for (int k = 0; k < hit.Length; k++) {
					int objectIndex = k;
					if ( getRootObject (hit [objectIndex].collider.gameObject) == rootNow) { //.name != "Terrain") ){
						if (i < xMin) {
							xMin = i;
							L = hit [objectIndex].point;
						}
						if (i > xMax) {
							xMax = i;
							R = hit [objectIndex].point;
						}
						if (j < yMin) {
							yMin = j;
							D = hit [objectIndex].point;
						}
						if (j > yMax) {
							yMax = j;
							U = hit [objectIndex].point;
						}
						break;
					}
				}
			}
		}

		UnityEngine.Object.DestroyImmediate (meshObj.GetComponent<MeshCollider> ().sharedMesh,true);
		UnityEngine.Rect r = new UnityEngine.Rect (xMin, yMin, xMax - xMin, yMax - yMin);
		return new PersonInfo (r, L, R, U, D);
	}

};

public class Person
{
	public Vector3 position;
	public Quaternion rotation;
	public int personIndex;
	public PersonInfo personInfo;
	public GameObject personObj;
	public GameObject emptyObj;
	public int stateIndex;
	public float stateNorTime;

	public Person (int imgWidth, int imgHight)
	{
		int imgL = (Screen.width - imgWidth) / 2;
		int imgR = Screen.width - imgL;


		position = new Vector3 (0f, 0f, UnityEngine.Random.Range(4f,9f));

		float rx = UnityEngine.Random.Range (-20f,20f);
		float rz = UnityEngine.Random.Range (-20f,20f);
		float ry = UnityEngine.Random.Range (-180f,180f);
		rotation = Quaternion.Euler(rx, ry, rz);
		emptyObj = new GameObject ("emtyObj");
		personIndex = UnityEngine.Random.Range (0, Global.personObjects.Length);
		personObj = (GameObject)UnityEngine.Object.Instantiate (Global.personObjects [personIndex], 
			                              position, rotation
			, emptyObj.transform);



		Animator ani = personObj.GetComponent<Animator> ();
		if (ani != null) {
			int clipsLen = ani.runtimeAnimatorController.animationClips.Length;
			stateIndex = UnityEngine.Random.Range (0, clipsLen);
			stateNorTime = UnityEngine.Random.value;
			ani.Play (ani.runtimeAnimatorController.animationClips [stateIndex].name, 0, stateNorTime);
			ani.speed = 0;
		} else {
			Animation anima = personObj.GetComponent<Animation> ();
			float totalLen = 0f;
			foreach (AnimationState state in anima) {
				totalLen += state.length;
			}
			stateIndex = 0;
			stateNorTime = UnityEngine.Random.value;
			float playTime  = stateNorTime*totalLen;
			anima ["Take 001"].time = playTime;
			anima.Play("Take 001");
			anima ["Take 001"].speed = 0f;
		}


		personInfo = Global.getRectFromPerson (personObj, emptyObj);
		Vector3 world_U = Global.toWorld (new Vector3 ((float)(personInfo.r.xMax + personInfo.r.xMin) / 2f, (float)imgHight, personInfo.U.z));
	
		Vector3 world_L = Global.toWorld (new Vector3 ((float)imgL, (float)(personInfo.r.yMax + personInfo.r.yMin) / 2f, personInfo.L.z));
		Vector3 world_D = Global.toWorld (new Vector3 ((float)(personInfo.r.xMax + personInfo.r.xMin) / 2f, 0f, personInfo.D.z));
		Vector3 world_R = Global.toWorld (new Vector3 ((float)imgR, (float)(personInfo.r.yMax + personInfo.r.yMin) / 2f, personInfo.R.z));
		float yMax = world_U.y - personInfo.U.y - 0.3f;
		float yMin = world_D.y - personInfo.D.y + 0.2f;
		float xMax = world_R.x - personInfo.R.x - 0.2f;
		float xMin = world_L.x - personInfo.L.x + 0.2f;
		if (yMax < yMin) {
			yMax = yMin = 0;
		}
		if (xMax < xMin) {
			xMax = xMin = 0;
		}
		float yR = UnityEngine.Random.Range (yMin, yMax);
		float xR = UnityEngine.Random.Range (xMin, xMax);
		personObj.transform.position += new Vector3(xR,yR,0f);
		personInfo = Global.getRectFromPerson (personObj, emptyObj);
	}
};

public class GenVector
{

	public float light_intensity;
	public float light_xAngle;
	public float light_yAngle;
	public int personNum;
	public List<Person> persons;
	public System.Random ran;
	public int imgHight;
	public int imgWidth;
	public int backgroundIndex;
	public GenVector ()
	{
		ran = new System.Random ();

	}

	public void setImgSize (int imgWidth0, int imgHight0)
	{
		imgHight = imgHight0;
		imgWidth = imgWidth0;
	}

	public static void swap<T>(ref T a, ref T b)
	{
		T t = a;
		a = b;
		b = t;
	}

	public bool overlap(Person p,Person t,float thr){
		float pM = p.personInfo.r.width * p.personInfo.r.height;
		float tM = t.personInfo.r.width * t.personInfo.r.height;

		float x1 = p.personInfo.r.xMin;
		float x3 = t.personInfo.r.xMin;

		float x2 = p.personInfo.r.xMax;
		float x4 = t.personInfo.r.xMax;

		float y1 = p.personInfo.r.xMin;
		float y3 = t.personInfo.r.xMin;

		float y2 = p.personInfo.r.xMax;
		float y4 = t.personInfo.r.xMax;
		float lx=Math.Max(x1,x3); float ly=Math.Max(y1,y3);
		float rx=Math.Min(x2,x4); float ry=Math.Min(y2,y4);
		if(lx>rx||ly>ry) return false;
		float oo = (rx - lx) * (ry - ly);
		
		return oo/pM > thr || oo/tM > thr;
	}

	public bool isGood(Person p){
		foreach (Person t in persons){
			if (overlap (p, t, 0.2f)) {
				return false;
			}
		}
		return true;
	}

	public void next ()
	{
		backgroundIndex = UnityEngine.Random.Range(1,Global.totalbackgroundNum+1);
		if (persons != null) {
			foreach(Person oo in persons){
				UnityEngine.Object.Destroy (oo.personObj);
				UnityEngine.Object.Destroy (oo.emptyObj);
			}
			persons = null;
		}
		personNum = UnityEngine.Random.Range (4, 6);
		
		light_intensity = UnityEngine.Random.Range (0.3f, 2.2f);
		light_xAngle = UnityEngine.Random.Range (-50f,50f);
		light_yAngle = UnityEngine.Random.Range (-50f,50f);
	}

	public void genPersons(){
		persons = new List<Person> ();
		persons.Clear ();
		for (int i = 0 ; i < personNum ; i++){
			Person p = new Person (imgWidth, imgHight);
			if (isGood (p)) {
				persons.Add (p);
			} else {
				i--;
				UnityEngine.Object.Destroy (p.personObj);
				UnityEngine.Object.Destroy (p.emptyObj);
				p = null;
			}
		}
	}
	public void print ()
	{

	}

	public void save(string fileName){
		FileStream fs = new FileStream(fileName, FileMode.Create);
		StreamWriter sw = new StreamWriter(fs);
		sw.Write (backgroundIndex+ " "+light_intensity+ " "+light_xAngle+ " "+light_yAngle+" "+personNum+" ");
		foreach (Person p in persons){
			sw.Write (  p.personIndex
				+ " " + p.personObj.transform.position.x
				+ " " + p.personObj.transform.position.y
				+ " " + p.personObj.transform.position.z
				+ " " + p.personObj.transform.rotation.x
				+ " " + p.personObj.transform.rotation.y
				+ " " + p.personObj.transform.rotation.z
				+ " " + p.stateIndex
				+ " " + p.stateNorTime
			);
		}
	
		sw.Flush();
		sw.Close();
		fs.Close();
	}

	public void load(string fileName){

	}


};

public class imageTest : MonoBehaviour
{
	
	private bool shoot;
	public int count;
	public GameObject background;

	private int imgNowWidth;
	public Color rectColor = Color.green;
	private Material rectMat = null;
	private GenVector genV;
	public GameObject[] personObjects;
	private bool shouldDestroy;
	public string backImgDir;
	public string vectorSaveDir;
	public string imgSaveDir;
	public string anoSaveDir;
	public GameObject light;
	public int totalbackgroundNum;
	DateTime beforDT;

	void Start ()
	{
		beforDT = System.DateTime.Now;  
		Global.totalbackgroundNum = totalbackgroundNum;
		light = transform.Find ("light").gameObject;
		Global.light = light;
		shoot = false;
		Shader shader = Shader.Find ("Lines/Colored Blended");
		rectMat = new Material (shader);
		genV = new GenVector ();
		Global.cam = this.GetComponent<Camera> ();
		Global.personObjects = personObjects;
		shouldDestroy = false;
		if (backImgDir == ""){
			backImgDir = "../../DATA/background/images/";
		}
		if (vectorSaveDir == ""){
			vectorSaveDir = "../../DATA/syntheticData/new/vectorSaveDir/";
		}
		if (imgSaveDir == ""){
			imgSaveDir = "../../DATA/syntheticData/new/imgSaveDir/";
		}
		if (anoSaveDir == ""){
			anoSaveDir = "../../DATA/syntheticData/new/anoSaveDir/";
		}
		Global.backImgDir = backImgDir;

	}

	void Update ()
	{
		if (shoot == false) {
			genV.next ();
			Global.light.transform.rotation = Quaternion.Euler (genV.light_xAngle,genV.light_yAngle,0f);
			Global.light.GetComponent<Light> ().intensity = genV.light_intensity;
			string imgpath = Global.backImgDir+genV.backgroundIndex+".jpg";
			Mat imgMat2 = Imgcodecs.imread (imgpath);
			Mat imgMat = new Mat ();
			int new_w, new_h = 3000;
			double scale = new_h / Convert.ToDouble (imgMat2.rows ());
			
			new_w = Math.Min( Convert.ToInt32 (imgMat2.cols () * scale)
				,Convert.ToInt32 (new_h/(float)Screen.height*Screen.width)-100 );
			imgNowWidth = new_w;
			Imgproc.resize (imgMat2, imgMat, new Size (Convert.ToDouble (new_w), Convert.ToDouble (new_h)));
			List<Mat> channels = new List<Mat> ();
			OpenCVForUnity.Core.split (imgMat, channels);
			Mat a = new Mat ();
			a = channels [0];
			channels [0] = channels [2];
			channels [2] = a;
			OpenCVForUnity.Core.merge (channels, imgMat);

			SpriteRenderer spr = background.GetComponent<SpriteRenderer> ();

			Texture2D texture2d = new Texture2D (new_w, new_h); 

			Utils.matToTexture2D (imgMat, texture2d);
			Sprite sp = Sprite.Create (texture2d, new UnityEngine.Rect (0, 0, new_w, new_h), new Vector2 (0.5f, 0.5f));//注意居中显示采用0.5f值  
			if (shouldDestroy) {
				DestroyImmediate(spr.sprite.texture,true);
				DestroyImmediate(spr.sprite,true);
			} else {
				shouldDestroy = true;
			}
			spr.sprite = sp;  

			int imgWW = Convert.ToInt32 (Screen.height / Convert.ToDouble (new_h) * new_w);
			Global.imgOnScrL = Convert.ToInt32 ((Screen.width- imgWW)/2f);
			Global.imgOnScrR = imgWW+Global.imgOnScrL ;
			
			genV.setImgSize ( imgWW, Screen.height);
			genV.genPersons ();
			genV.print ();
			shoot = true;
		}
	}

	void OnPostRender ()
	{
		if(shoot){
			ArrayList rects = new ArrayList();//findPersonByTag("person");
			foreach (Person p in genV.persons){
				PersonInfo r = Global.getRectFromPerson (p.personObj,p.emptyObj);
				rects.Add (r.r);
			}

			count=count+1;


			string fileName = count.ToString();
			int shootWidth = Convert.ToInt32((Screen.height / 3000.0) * imgNowWidth);
			genV.save (vectorSaveDir+fileName+".txt");
			rect2txt_acf (rects,anoSaveDir+fileName+".txt");
			CaptureCamera(new UnityEngine.Rect( (Screen.width-shootWidth)/2,0,shootWidth,Screen.height),imgSaveDir+fileName+".jpg");
			shoot = false;
			DateTime afterDT = System.DateTime.Now;  
			TimeSpan ts = afterDT.Subtract(beforDT);  
			Debug.Log (count +" total time:"+ts.TotalSeconds +"s, average time:"+ts.TotalSeconds/count+"s");
		}
	}

	void rect2txt_acf(ArrayList rects , string name){
		FileStream fs = new FileStream(name, FileMode.Create);

		StreamWriter sw = new StreamWriter(fs);
		
		sw.Write("% bbGt version=3\n");
		foreach(UnityEngine.Rect r in rects){
			sw.Write("person "+Convert.ToInt32 (r.x-Global.imgOnScrL)+ " " + Convert.ToInt32 (Screen.height-r.yMax) + " " + Convert.ToInt32 (r.width) +" " + Convert.ToInt32 (r.height) +" 0 0 0 0 0 0 0\n");
		}

		
		sw.Flush();
		sw.Close();
		fs.Close();
	}

	Vector3 toScreen (Vector3 v)
	{
		return  this.GetComponent<Camera> ().WorldToScreenPoint (v);
	}



	void drawRects (ArrayList rects)
	{
		if (!rectMat)
			return;
		GL.PushMatrix ();
		rectMat.SetPass (0);

		GL.LoadPixelMatrix ();

		GL.Begin (GL.LINES);
		
		foreach (UnityEngine.Rect rect0 in rects) {
			
			float startX = rect0.x;
			float startY = rect0.y;
			float endX = rect0.xMax;
			float endY = rect0.yMax;

			GL.Color (rectColor);

			GL.Vertex3 (startX, startY, 0);
			GL.Vertex3 (endX, startY, 0);

			GL.Vertex3 (endX, startY, 0);
			GL.Vertex3 (endX, endY, 0);

			GL.Vertex3 (endX, endY, 0);
			GL.Vertex3 (startX, endY, 0);

			GL.Vertex3 (startX, endY, 0);
			GL.Vertex3 (startX, startY, 0);

		}
		GL.End ();
		GL.PopMatrix ();
	}

	void CaptureCamera ( UnityEngine.Rect rect, string name = "")
	{  
		Texture2D screenShot = new Texture2D ((int)rect.width, (int)rect.height, TextureFormat.RGB24, false);  
		screenShot.ReadPixels (rect, 0, 0);
		screenShot.Apply ();  
		byte[] bytes = screenShot.EncodeToPNG ();  
		string filename;
		if (name == "") {
			filename = Application.dataPath + "/Screenshot.png";  
		} else {
			filename = name;
		}
		System.IO.File.WriteAllBytes (filename, bytes);  
		DestroyImmediate (screenShot,true);
	}
}
