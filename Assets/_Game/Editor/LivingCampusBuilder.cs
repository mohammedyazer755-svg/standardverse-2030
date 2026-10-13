using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEditor;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEditor.SceneManagement;
using static CampusPulse.Editor.CampusBuilder;

namespace CampusPulse.Editor
{
    public static class LivingCampusBuilder
    {
        static Material stone, trim, glass, roof, metal, lawn, paving, road, bark, leaves, leavesLight, white, aqua;
        static readonly List<Transform> trees = new List<Transform>();
        static readonly List<Transform> rotors = new List<Transform>();
        static readonly List<Renderer> lamps = new List<Renderer>();
        static readonly List<FacilityView> facilities = new List<FacilityView>();
        static int treeIndex;
        [MenuItem("Campus Pulse/Generate Academic Benchmark")]
        public static void Benchmark() { Generate(false); }
        [MenuItem("Campus Pulse/Generate Living Campus")]
        public static void FullCampus() { Generate(true); }
        static void Generate(bool full)
        {
            BeginScene(); trees.Clear(); rotors.Clear(); lamps.Clear(); facilities.Clear(); treeIndex=0;
            Directory.CreateDirectory("Assets/_Game/Art/Meshes"); Directory.CreateDirectory("Assets/_Game/Prefabs");
            stone=Material("Warm limestone",new Color(.76f,.74f,.66f),0,.3f);
            trim=Material("Pale concrete",new Color(.87f,.86f,.8f),0,.25f);
            glass=Material("Blue reflective glazing",new Color(.12f,.26f,.31f),.65f,.88f);
            roof=Material("Roof gravel",new Color(.3f,.33f,.33f),0,.15f);
            metal=Material("Graphite metal",new Color(.11f,.15f,.17f),.7f,.45f);
            lawn=Material("Lawn",new Color(.22f,.34f,.15f),0,.15f);
            paving=Material("Paving",new Color(.55f,.56f,.51f),0,.2f);
            road=Material("Asphalt",new Color(.105f,.13f,.14f),0,.12f);
            bark=Material("Tree bark",new Color(.23f,.18f,.12f),0,.1f);
            leaves=Material("Foliage olive",new Color(.24f,.37f,.12f),0,.15f);
            leavesLight=Material("Foliage sage",new Color(.33f,.43f,.19f),0,.15f);
            white=Material("Road paint",new Color(.8f,.8f,.69f));
            aqua=Material("Selection cyan",new Color(.03f,.75f,.83f),.2f,.5f);
            var groundRoot=new GameObject("Campus landscape").transform;
            Box("Landscape base",new Vector3(0,-.35f,0),new Vector3(230,.6f,185),lawn,groundRoot);
            Box("Foundation perimeter",new Vector3(0,-1.1f,0),new Vector3(232,1.1f,187),stone,groundRoot);
            var academic=Building("academic","School of Engineering","Teaching studios, lecture rooms and a shaded academic courtyard.",new Vector3(-30,0,12),34,19,4,true);
            facilities.Add(academic);
            LandscapeBlock(academic,34,19);
            if(!full)
            {
                facilities.Add(Building("library","Learning Commons","Library and collaborative study rooms.",new Vector3(32,0,20),24,16,3,false));
                facilities.Add(Building("canteen","Campus Kitchen","Dining hall and student services.",new Vector3(28,0,-26),26,16,1,false));
                LandscapeBlock(facilities[1],24,16); LandscapeBlock(facilities[2],26,16);
            }
            else
            {
                // Expanded only after the representative block has been rendered and reviewed.
                AddBuilding("research","Innovation Labs","Engineering laboratories and research facilities.",new Vector3(-30,0,57),32,19,3);
                AddBuilding("library","Learning Commons","Library, quiet study and collaborative learning.",new Vector3(22,0,52),26,20,3);
                AddBuilding("administration","Campus House","Administration and student support services.",new Vector3(-78,0,10),23,17,2);
                AddBuilding("canteen","Campus Kitchen","Dining hall, kitchen and student meeting point.",new Vector3(24,0,7),26,18,1);
                AddBuilding("auditorium","Convocation Hall","Auditorium and major campus gatherings.",new Vector3(73,0,46),30,24,2);
                AddBuilding("hostel","Student Residences","Student housing with shared living spaces.",new Vector3(75,0,1),25,22,5);
            }
            RoadNetwork(groundRoot,full);
            if(full) { Utilities(); Parking(); Gardens(); }
            else Gardens();
            CombineStatic(groundRoot,"landscape");
            Complete(facilities,full?290:130);
            var session=Object.FindFirstObjectByType<CampusSession>();
            session.Rig.Bounds=new Vector2(93,70); session.Rig.Overview=full?new Vector3(0,0,-4):new Vector3(0,0,5);
            var life=new GameObject("Living campus presentation").AddComponent<CampusLife>(); life.Session=session;
            life.WalkerPrefab=WalkerPrefab(); life.Trees=trees.ToArray(); life.Rotors=rotors.ToArray(); life.Lamps=lamps.ToArray();
            var solarTables=new List<Transform>();
            foreach(var t in Object.FindObjectsByType<Transform>(FindObjectsSortMode.None)) if(t.name=="Solar table") solarTables.Add(t);
            life.SolarTables=solarTables.ToArray();
            life.OutfitMaterials=new[]{Material("Student teal",new Color(.12f,.43f,.48f)),Material("Student ochre",new Color(.65f,.37f,.12f)),Material("Student blue",new Color(.19f,.27f,.44f)),Material("Student burgundy",new Color(.4f,.16f,.19f))};
            life.Vehicle=ServiceVehicle(); life.VehicleRoute=new[]{new Vector3(-98,.2f,-48),new Vector3(98,.2f,-48),new Vector3(98,.2f,75),new Vector3(-98,.2f,75)};
            life.Vehicle.position=life.VehicleRoute[0];
            foreach(var id in AssetDatabase.FindAssets("t:UniversalRenderPipelineAsset",new[]{"Assets/Settings"}))
            {
                var pipeline=AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(AssetDatabase.GUIDToAssetPath(id));
                pipeline.shadowDistance=280; pipeline.msaaSampleCount=2; EditorUtility.SetDirty(pipeline);
            }
            EditorSceneManager.SaveScene(SceneManager.GetActiveScene(),ScenePath); AssetDatabase.SaveAssets();
            Debug.Log("CAMPUS_LIVING_GENERATED " + (full?"Full campus":"Academic benchmark"));
        }
        static void AddBuilding(string id,string name,string purpose,Vector3 p,float w,float d,int floors)
        {
            var f=Building(id,name,purpose,p,w,d,floors,false); facilities.Add(f); LandscapeBlock(f,w,d);
        }
        static FacilityView Building(string id,string name,string purpose,Vector3 p,float width,float depth,int floors,bool wings)
        {
            var root=new GameObject(name); root.transform.position=p;
            var f=root.AddComponent<FacilityView>(); f.Id=id; f.DisplayName=name; f.Purpose=purpose;
            f.Capacity=floors*100; f.InitialOccupants=floors*65; f.Water=floors*7.5f; f.Power=floors*22; f.Waste=floors*6.5f;
            var visual=new GameObject("Architecture").transform; visual.SetParent(root.transform,false);
            float height=floors*3.5f;
            LocalBox(visual,"Raised plinth",new Vector3(0,.3f,0),new Vector3(width+2,.6f,depth+2),trim);
            Wing(visual,Vector3.zero,width,depth,floors);
            if(wings)
            {
                Wing(visual,new Vector3(-width*.5f+4,0,-depth*.5f-6),8,12,floors-1);
                Wing(visual,new Vector3(width*.5f-4,0,-depth*.5f-6),8,12,floors-1);
            }
            LocalBox(visual,"Entrance portal",new Vector3(0,2,-depth*.5f-.45f),new Vector3(7,3.5f,.9f),metal);
            LocalBox(visual,"Entrance glazing",new Vector3(0,2,-depth*.5f-.95f),new Vector3(5.8f,3,.12f),glass);
            LocalBox(visual,"Glass canopy",new Vector3(0,4.1f,-depth*.5f-2),new Vector3(9,.16f,4),glass);
            for(int i=-1;i<=1;i+=2) LocalBox(visual,"Canopy pillar",new Vector3(i*3.7f,2,-depth*.5f-3.6f),new Vector3(.16f,4,.16f),metal);
            for(int i=0;i<3;i++) LocalBox(visual,"Entry stair",new Vector3(0,.12f+i*.11f,-depth*.5f-4+i*.65f),new Vector3(8,.18f,1.6f),trim);
            Sign(root.transform,name.ToUpperInvariant(),new Vector3(0,4.8f,-depth*.5f-.7f),.18f,Color.white);
            CombineStatic(visual,id);
            f.ActivityIndicator=LocalBox(root.transform,"Service status glow",new Vector3(0,4.45f,-depth*.5f-1),new Vector3(2,.18f,.15f),aqua).GetComponent<Renderer>();
            float boundDepth=depth+(wings?12:0); float centerZ=wings?-6:0;
            var pin=new GameObject("Digital Twin Pin").transform; pin.SetParent(root.transform,false);
            pin.localPosition=new Vector3(0,height+3.2f,centerZ);
            var pinGlow=Shape(PrimitiveType.Cylinder,"Pin Glow",pin,Vector3.zero,new Vector3(1.3f,.15f,1.3f),aqua);
            pinGlow.transform.localRotation=Quaternion.Euler(90,0,0);
            f.StatusPinGlow=pinGlow.GetComponent<Renderer>();
            Sign(pin,name.ToUpperInvariant(),new Vector3(0,1.2f,0),.16f,Color.white);
            var collider=root.AddComponent<BoxCollider>(); collider.center=new Vector3(0,height*.5f,centerZ); collider.size=new Vector3(width,height,boundDepth);
            f.FocusOffset=new Vector3(0,2,centerZ); f.FocusDistance=Mathf.Max(65,width*2.3f);
            var marker=new GameObject("Selection footprint").transform; marker.SetParent(root.transform,false);
            float mz=centerZ; float mw=width+3,md=boundDepth+3;
            LocalBox(marker,"Edge",new Vector3(0,.37f,mz-md/2),new Vector3(mw,.08f,.15f),aqua);
            LocalBox(marker,"Edge",new Vector3(0,.37f,mz+md/2),new Vector3(mw,.08f,.15f),aqua);
            LocalBox(marker,"Edge",new Vector3(-mw/2,.37f,mz),new Vector3(.15f,.08f,md),aqua);
            LocalBox(marker,"Edge",new Vector3(mw/2,.37f,mz),new Vector3(.15f,.08f,md),aqua);
            CombineStatic(marker,id+"-selection"); f.Highlight=marker.GetComponentInChildren<Renderer>(); f.Highlight.gameObject.SetActive(false);
            PrefabUtility.SaveAsPrefabAsset(root,"Assets/_Game/Prefabs/"+id+".prefab");
            return f;
        }
        static void Wing(Transform parent,Vector3 offset,float w,float d,int floors)
        {
            float h=floors*3.5f;
            LocalBox(parent,"Masonry core",offset+new Vector3(0,h/2+.5f,0),new Vector3(w,h,d),stone);
            LocalBox(parent,"Roof surface",offset+new Vector3(0,h+.9f,0),new Vector3(w-.6f,.12f,d-.6f),roof);
            for(int level=0;level<=floors;level++)
                LocalBox(parent,"Floor band",offset+new Vector3(0,.7f+level*3.5f,0),new Vector3(w+.35f,.28f,d+.35f),trim);
            for(int side=-1;side<=1;side+=2)
            {
                int bays=Mathf.FloorToInt(w/3.2f);
                for(int b=0;b<bays;b++)
                {
                    float x=-w/2+(b+.5f)*w/bays;
                    for(int floor=0;floor<floors;floor++)
                    {
                        float y=2.15f+floor*3.5f;
                        LocalBox(parent,"Window reveal",offset+new Vector3(x,y,side*(d/2+.035f)),new Vector3(2.55f,2.45f,.16f),metal);
                        LocalBox(parent,"Recessed glazing",offset+new Vector3(x,y,side*(d/2+.13f)),new Vector3(2.26f,2.17f,.06f),glass);
                        LocalBox(parent,"Mullion",offset+new Vector3(x,y,side*(d/2+.19f)),new Vector3(.075f,2.3f,.12f),trim);
                    }
                    LocalBox(parent,"Solar shading fin",offset+new Vector3(x-1.45f,h/2+.5f,side*(d/2+.4f)),new Vector3(.2f,h,.9f),trim);
                }
                int sideBays=Mathf.FloorToInt(d/3.2f);
                for(int b=0;b<sideBays;b++) for(int floor=0;floor<floors;floor++)
                {
                    float z=-d/2+(b+.5f)*d/sideBays;
                    LocalBox(parent,"Side glazing",offset+new Vector3(side*(w/2+.07f),2.15f+floor*3.5f,z),new Vector3(.16f,2.2f,2.15f),glass);
                    LocalBox(parent,"Side fin",offset+new Vector3(side*(w/2+.3f),2.15f+floor*3.5f,z-1.3f),new Vector3(.7f,3.1f,.18f),trim);
                }
                LocalBox(parent,"Roof parapet",offset+new Vector3(0,h+.9f,side*d/2),new Vector3(w+.3f,.7f,.3f),trim);
                LocalBox(parent,"Roof parapet",offset+new Vector3(side*w/2,h+.9f,0),new Vector3(.3f,.7f,d),trim);
            }
            for(int i=-1;i<=1;i+=2)
            {
                LocalBox(parent,"Rooftop plant",offset+new Vector3(i*w*.25f,h+1.1f,1),new Vector3(3.2f,1.1f,2.2f),metal);
                for(int slat=0;slat<7;slat++) LocalBox(parent,"Plant louver",offset+new Vector3(i*w*.25f,h+1.7f,slat*.25f+.2f),new Vector3(3,.05f,.08f),trim);
            }
        }
        static void LandscapeBlock(FacilityView f,float w,float d)
        {
            var root=new GameObject(f.Id+" landscape").transform; root.position=f.transform.position;
            bool academic=f.Id=="academic"; float extra=academic?12:0;
            LocalBox(root,"Pedestrian apron",new Vector3(0,.02f,-extra/2),new Vector3(w+10,.25f,d+extra+10),paving);
            for(int i=-1;i<=1;i+=2)
            {
                var p=f.transform.position+new Vector3(i*(w/2+5),0,-d/2-extra-5);
                Tree(p,5.7f); Bench(p+new Vector3(-i*2.5f,0,-1),root); Lamp(p+new Vector3(0,0,-4));
            }
            float front=f.transform.position.z-d/2-extra-8;
            for(int i=-1;i<=1;i+=2)
            {
                var p=new Vector3(f.transform.position.x+i*7,0,front);
                Box("Planter",p+Vector3.up*.45f,new Vector3(6,.6f,3),trim,root);
                Box("Planted bed",p+Vector3.up*.8f,new Vector3(5.7f,.3f,2.7f),leaves,root);
            }
            // Segregated recycling stations at entrance
            for(int b=-1;b<=1;b++)
            {
                var binColor=b<0?new Color(.12f,.45f,.72f):b==0?new Color(.14f,.55f,.25f):new Color(.85f,.65f,.12f);
                LocalBox(root,"Recycling bin "+b,new Vector3(b*.65f+7f,.42f,-d/2-extra-3.5f),new Vector3(.4f,.84f,.4f),Material("CampusSortBin "+b,binColor));
            }
            if(academic||f.Id=="library")
            {
                for(int k=0;k<4;k++)
                    LocalBox(root,"Bike rack "+k,new Vector3(-9f+k*1.1f,.38f,-d/2-extra-3.5f),new Vector3(.08f,.75f,1.1f),metal);
            }
            CombineStatic(root,f.Id+"-landscape");
        }
        static void RoadNetwork(Transform root,bool full)
        {
            foreach(float z in new[]{-48f,75f})
            {
                Box("Campus avenue",new Vector3(0,.03f,z),new Vector3(205,.18f,8),road,root);
                for(int x=-95;x<100;x+=8) Box("Lane dash",new Vector3(x,.13f,z),new Vector3(3,.02f,.13f),white,root);
                for(int side=-1;side<=1;side+=2) Box("Avenue sidewalk",new Vector3(0,.16f,z+side*5.3f),new Vector3(208,.32f,2.2f),paving,root);
            }
            foreach(float x in new[]{-98f,98f})
            {
                Box("Campus loop",new Vector3(x,.03f,13),new Vector3(8,.18f,124),road,root);
                for(int z=-40;z<75;z+=8) Box("Lane dash",new Vector3(x,.13f,z),new Vector3(.13f,.02f,3),white,root);
                for(int side=-1;side<=1;side+=2) Box("Loop sidewalk",new Vector3(x+side*5.3f,.16f,13),new Vector3(2.2f,.32f,126),paving,root);
            }
            Box("Central pedestrian spine",new Vector3(-3,.15f,10),new Vector3(6,.3f,123),paving,root);
            Box("East west pedestrian spine",new Vector3(0,.15f,-22),new Vector3(196,.3f,5),paving,root);
            Box("North pedestrian spine",new Vector3(0,.15f,33),new Vector3(196,.3f,5),paving,root);
            for(int i=0;i<7;i++) Box("Crosswalk",new Vector3(-5+i*1.6f,.14f,-48),new Vector3(.8f,.02f,7),white,root);
            for(int x=-88;x<=88;x+=16)
            {
                Tree(new Vector3(x,0,-57),6); Tree(new Vector3(x,0,84),5.5f);
                Lamp(new Vector3(x,0,-42));
            }
            for(int z=-32;z<70;z+=17) { Tree(new Vector3(-108,0,z),6.5f); Tree(new Vector3(108,0,z),6); }
            Box("Entrance gate left",new Vector3(-12,2,-57),new Vector3(2,4,2),stone,root);
            Box("Entrance gate right",new Vector3(12,2,-57),new Vector3(2,4,2),stone,root);
            Box("Entrance lintel",new Vector3(0,4.3f,-57),new Vector3(26,.8f,1.4f),metal,root);
            Sign(root,"C A M P U S   P U L S E   /   2 0 3 0",new Vector3(0,4.35f,-57.8f),.17f,Color.white,true);
        }
        static void Gardens()
        {
            var root=new GameObject("Courtyard gardens").transform;
            for(int i=0;i<5;i++)
            {
                var p=new Vector3(-84+i*7,0,53);
                Tree(p,5.5f+(i%2)); Bench(p+new Vector3(0,0,-4),root);
                LocalBox(root,"Garden bed",p+new Vector3(0,.15f,0),new Vector3(6,.3f,5),leavesLight);
            }
            CombineStatic(root,"gardens");
            if(facilities.Count>3)
            {
                var garden=Utility("garden","Reflection Garden","Shaded outdoor space and an alternate student gathering zone.",new Vector3(-70,0,53),37,22);
                garden.Capacity=240; garden.InitialOccupants=100;
                garden.Water=5; garden.Power=1; garden.Waste=5;
            }
        }
        static void Tree(Vector3 p,float h)
        {
            var root=new GameObject("Tree "+treeIndex++).transform; root.position=p;
            Shape(PrimitiveType.Cylinder,"Trunk",root,new Vector3(0,h*.28f,0),new Vector3(.45f,h*.28f,.45f),bark);
            var crown=new GameObject("Wind crown").transform; crown.SetParent(root,false); crown.localPosition=new Vector3(0,h*.5f,0);
            for(int i=0;i<5;i++)
            {
                float a=i*2.4f;
                Shape(PrimitiveType.Sphere,"Canopy cluster",crown,new Vector3(Mathf.Cos(a)*h*.18f,(i%3)*h*.09f,Mathf.Sin(a)*h*.18f),new Vector3(h*.55f,h*.53f,h*.5f),i%2==0?leaves:leavesLight);
            }
            // Shared generated mesh across all identical-height trees; foliage moves as one crown.
            CombineStatic(crown,"tree-crown-"+h.ToString("0.0",System.Globalization.CultureInfo.InvariantCulture));
            trees.Add(crown);
        }
        static void Bench(Vector3 p,Transform parent)
        {
            var wood=Material("Bench timber",new Color(.44f,.27f,.12f));
            for(int i=0;i<4;i++) Box("Bench slat",p+new Vector3(0,.65f,i*.16f),new Vector3(2.4f,.12f,.12f),wood,parent);
            Box("Bench back",p+new Vector3(0,1,.5f),new Vector3(2.4f,.55f,.1f),wood,parent);
            for(int i=-1;i<=1;i+=2) Box("Bench leg",p+new Vector3(i*.9f,.3f,.25f),new Vector3(.13f,.6f,.5f),metal,parent);
        }
        static void Lamp(Vector3 p)
        {
            var root=new GameObject("Pedestrian light").transform; root.position=p;
            Shape(PrimitiveType.Cylinder,"Pole",root,new Vector3(0,2.5f,0),new Vector3(.12f,2.5f,.12f),metal);
            LocalBox(root,"Light arm",new Vector3(.45f,4.95f,0),new Vector3(1,.12f,.16f),metal);
            var head=LocalBox(root,"Light glow",new Vector3(.85f,4.87f,0),new Vector3(.7f,.1f,.4f),Material("Lamp glow",new Color(1,.84f,.5f)));
            lamps.Add(head.GetComponent<Renderer>());
        }
        static GameObject WalkerPrefab()
        {
            var root=new GameObject("Student");
            var cloth=Material("Student teal",new Color(.12f,.43f,.48f)); var skin=Material("Student skin",new Color(.48f,.3f,.19f));
            var trousers=Material("Student trousers",new Color(.11f,.14f,.2f));
            Shape(PrimitiveType.Capsule,"Torso",root.transform,new Vector3(0,1.15f,0),new Vector3(.38f,.34f,.26f),cloth);
            Shape(PrimitiveType.Sphere,"Head",root.transform,new Vector3(0,1.68f,0),new Vector3(.29f,.34f,.28f),skin);
            for(int side=-1;side<=1;side+=2)
            {
                var leg=new GameObject(side<0?"Left leg":"Right leg").transform; leg.SetParent(root.transform,false); leg.localPosition=new Vector3(side*.1f,.91f,0);
                Shape(PrimitiveType.Capsule,"Trousers",leg,new Vector3(0,-.36f,0),new Vector3(.14f,.37f,.16f),trousers);
                var arm=new GameObject(side<0?"Left arm":"Right arm").transform; arm.SetParent(root.transform,false); arm.localPosition=new Vector3(side*.25f,1.4f,0);
                Shape(PrimitiveType.Capsule,"Sleeve",arm,new Vector3(0,-.28f,0),new Vector3(.12f,.28f,.12f),cloth);
            }
            LocalBox(root.transform,"Backpack",new Vector3(0,1.15f,-.19f),new Vector3(.29f,.4f,.15f),trousers);
            var prefab=PrefabUtility.SaveAsPrefabAsset(root,"Assets/_Game/Prefabs/Student.prefab"); Object.DestroyImmediate(root); return prefab;
        }
        static Transform ServiceVehicle()
        {
            var root=new GameObject("Electric campus service vehicle").transform;
            LocalBox(root,"Chassis",new Vector3(0,.7f,0),new Vector3(1.7f,.45f,3.5f),metal);
            LocalBox(root,"Cabin",new Vector3(0,1.45f,.85f),new Vector3(1.6f,1.15f,1.3f),trim);
            LocalBox(root,"Windshield",new Vector3(0,1.62f,1.53f),new Vector3(1.4f,.65f,.04f),glass);
            LocalBox(root,"Collection bed",new Vector3(0,1.3f,-.85f),new Vector3(1.6f,.9f,1.7f),Material("Service teal",new Color(.06f,.4f,.39f)));
            for(int x=-1;x<=1;x+=2) for(int z=-1;z<=1;z+=2)
            {
                var wheel=Shape(PrimitiveType.Cylinder,"Wheel",root,new Vector3(x*.87f,.5f,z*1.15f),new Vector3(.7f,.12f,.7f),metal);
                wheel.transform.localRotation=Quaternion.Euler(0,0,90);
            }
            return root;
        }
        static void Utilities()
        {
            var water=Utility("water","Water & Pump House","Elevated industrial reservoir towers and electrically driven pumping.",new Vector3(-66,0,-71),29,20);
            var waterMat=Material("Reservoir water",new Color(.05f,.43f,.55f),.45f,.92f);
            var valveMat=Material("Valve red",new Color(.85f,.14f,.12f),.3f,.5f);
            for(int i=-1;i<=1;i+=2)
            {
                // Concrete piers and 4 steel truss stanchion legs
                for(int x=-1;x<=1;x+=2) for(int z=-1;z<=1;z+=2)
                {
                    LocalBox(water.transform,"Pier",new Vector3(i*7+x*2.7f,.25f,2+z*2.7f),new Vector3(.9f,.5f,.9f),trim);
                    LocalBox(water.transform,"Tower leg",new Vector3(i*7+x*2.7f,3f,2+z*2.7f),new Vector3(.24f,5.6f,.24f),metal);
                }
                // Structural X-braces
                LocalBox(water.transform,"Cross brace front",new Vector3(i*7,3f,2-2.7f),new Vector3(5.2f,.12f,.12f),metal);
                LocalBox(water.transform,"Cross brace back",new Vector3(i*7,3f,2+2.7f),new Vector3(5.2f,.12f,.12f),metal);

                // Elevated catwalk platform with safety railing at height 5.8m
                LocalBox(water.transform,"Catwalk platform",new Vector3(i*7,5.8f,2),new Vector3(7.6f,.18f,7.6f),metal);
                for(int side=-1;side<=1;side+=2)
                {
                    LocalBox(water.transform,"Railing X",new Vector3(i*7,6.4f,2+side*3.7f),new Vector3(7.6f,.9f,.08f),metal);
                    LocalBox(water.transform,"Railing Z",new Vector3(i*7+side*3.7f,6.4f,2),new Vector3(.08f,.9f,7.6f),metal);
                }

                // Elevated tank body sitting atop the catwalk
                var tank=new GameObject("Tank "+i,typeof(MeshFilter),typeof(MeshRenderer));
                tank.transform.SetParent(water.transform,false);
                tank.transform.localPosition=new Vector3(i*7,5.9f,2);
                tank.GetComponent<MeshFilter>().sharedMesh=TankMesh();
                tank.GetComponent<Renderer>().sharedMaterial=trim;

                // Conical steel roof cap and structural rib rings
                Shape(PrimitiveType.Cylinder,"Tank roof",water.transform,new Vector3(i*7,11.5f,2),new Vector3(7.8f,.35f,7.8f),metal);
                for(int r=1;r<=3;r++) Shape(PrimitiveType.Cylinder,"Tank rib",water.transform,new Vector3(i*7,5.9f+r*1.35f,2),new Vector3(7.85f,.1f,7.85f),metal);

                // Water surface inside the tank
                var surface=Shape(PrimitiveType.Cylinder,"Stored water",tank.transform,new Vector3(0,3.3f,0),new Vector3(7.3f,.06f,7.3f),waterMat);
                if(i<0) water.WaterSurface=surface.transform;

                // Main Riser Pipe descending to ground
                LocalBox(water.transform,"Main riser pipe",new Vector3(i*7,2.9f,2),new Vector3(.5f,5.8f,.5f),aqua);
                // Shutoff valve handwheel at eye level
                var wheel=Shape(PrimitiveType.Cylinder,"Valve handwheel",water.transform,new Vector3(i*7,1.3f,1.55f),new Vector3(.55f,.08f,.55f),valveMat);
                wheel.transform.localRotation=Quaternion.Euler(90,0,0);

                // Service ladder
                for(int rung=0;rung<16;rung++) LocalBox(water.transform,"Service ladder",new Vector3(i*7+3.85f,.4f+rung*.36f,2),new Vector3(.65f,.08f,.12f),metal);
                LocalBox(water.transform,"Tank gauge",new Vector3(i*7,8.6f,-2.1f),new Vector3(.35f,4.7f,.16f),aqua);
            }
            LocalBox(water.transform,"Pump house",new Vector3(0,1.5f,-6.4f),new Vector3(7,3,4),stone);
            LocalBox(water.transform,"Pump roof",new Vector3(0,3.2f,-6.4f),new Vector3(7.5f,.3f,4.5f),metal);
            LocalBox(water.transform,"Manifold",new Vector3(0,.7f,-2),new Vector3(15,.35f,.35f),aqua);
            var rotor=Shape(PrimitiveType.Cylinder,"Pump status rotor",water.transform,new Vector3(0,1.7f,-8.5f),new Vector3(1.1f,.1f,1.1f),aqua);
            rotor.transform.localRotation=Quaternion.Euler(90,0,0); rotors.Add(rotor.transform);
            LocalBox(rotor.transform,"Rotor needle",Vector3.zero,new Vector3(.13f,.3f,.9f),white);

            // Active leak puddle on the ground
            var leak=LocalBox(water.transform,"Active pipe leak",new Vector3(0,.05f,-3.6f),new Vector3(4.8f,.04f,3.8f),waterMat);
            water.LeakIndicator=leak.GetComponent<Renderer>();

            var power=Utility("power","Campus Substation","Grid distribution, building services and limited backup supply.",new Vector3(-24,0,-71),27,20);
            for(int i=-1;i<=1;i+=2)
            {
                LocalBox(power.transform,"Transformer",new Vector3(i*6,2,0),new Vector3(5,4,4),metal);
                for(int j=0;j<8;j++) LocalBox(power.transform,"Cooling fin",new Vector3(i*6-2.7f+j*.75f,2,-2.3f),new Vector3(.18f,3.5f,1),trim);
                for(int j=-1;j<=1;j++) Shape(PrimitiveType.Cylinder,"Insulator",power.transform,new Vector3(i*6+j*1.4f,4.6f,0),new Vector3(.35f,.65f,.35f),stone);
            }
            var solar=Utility("solar","Solar Garden","A campus photovoltaic array. Generation follows game daylight.",new Vector3(20,0,-71),32,20);
            var cell=Material("Photovoltaic cells",new Color(.035f,.11f,.21f),.6f,.8f);
            for(int row=0;row<3;row++) for(int col=0;col<5;col++)
            {
                var pivot=new GameObject("Solar table").transform; pivot.SetParent(solar.transform,false); pivot.localPosition=new Vector3(-12+col*6,2,-6+row*6); pivot.localRotation=Quaternion.Euler(-22,0,0);
                LocalBox(pivot,"Panel frame",Vector3.zero,new Vector3(5.4f,.15f,3.8f),trim);
                LocalBox(pivot,"PV surface",new Vector3(0,.1f,0),new Vector3(5.2f,.06f,3.6f),cell);
                for(int line=-2;line<=2;line++) LocalBox(pivot,"Cell grid",new Vector3(line,.15f,0),new Vector3(.025f,.01f,3.6f),metal);
                LocalBox(solar.transform,"Support",new Vector3(-12+col*6,1,-6+row*6),new Vector3(.2f,2,.2f),metal);
            }
            var waste=Utility("recycling","Recovery Yard","Segregated collection, recycling and tracked waste disposal.",new Vector3(69,0,-71),31,20);
            var colors=new[]{new Color(.14f,.48f,.34f),new Color(.17f,.33f,.55f),new Color(.62f,.45f,.13f)};
            for(int i=0;i<3;i++)
            {
                var m=Material("Collection bin "+i,colors[i]);
                LocalBox(waste.transform,"Sorting container",new Vector3(-9+i*9,1.5f,0),new Vector3(7,3,5),m);
                LocalBox(waste.transform,"Container lid",new Vector3(-9+i*9,3.1f,0),new Vector3(7.3f,.2f,5.3f),metal);
            }
            foreach(var f in new[]{water,power,solar,waste})
            {
                var root=new GameObject(f.Id+" access path").transform;
                Box("Service path",new Vector3(f.transform.position.x,.15f,-57),new Vector3(4,.3f,16),paving,root);
                CombineStatic(root,f.Id+"-access");
                Sign(f.transform,f.DisplayName.ToUpperInvariant(),new Vector3(0,5,-10.3f),.16f,Color.white);
            }
        }
        static FacilityView Utility(string id,string name,string purpose,Vector3 p,float w,float d)
        {
            var root=new GameObject(name); root.transform.position=p;
            var f=root.AddComponent<FacilityView>(); f.Id=id; f.DisplayName=name; f.Purpose=purpose; f.Capacity=40; f.InitialOccupants=5; f.Water=1; f.Power=3; f.Waste=2; f.FocusDistance=70;
            var c=root.AddComponent<BoxCollider>(); c.center=new Vector3(0,3,0); c.size=new Vector3(w,6,d);
            var pin=new GameObject("Digital Twin Pin").transform; pin.SetParent(root.transform,false);
            pin.localPosition=new Vector3(0,7.2f,0);
            var pinGlow=Shape(PrimitiveType.Cylinder,"Pin Glow",pin,Vector3.zero,new Vector3(1.3f,.15f,1.3f),aqua);
            pinGlow.transform.localRotation=Quaternion.Euler(90,0,0);
            f.StatusPinGlow=pinGlow.GetComponent<Renderer>();
            Sign(pin,name.ToUpperInvariant(),new Vector3(0,1.2f,0),.16f,Color.white);
            if(id!="garden") LocalBox(root.transform,"Service apron",new Vector3(0,.08f,0),new Vector3(w+3,.16f,d+3),paving);
            var marker=new GameObject("Selection").transform; marker.SetParent(root.transform,false);
            LocalBox(marker,"Front",new Vector3(0,.27f,-d/2),new Vector3(w,.08f,.18f),aqua);
            LocalBox(marker,"Back",new Vector3(0,.27f,d/2),new Vector3(w,.08f,.18f),aqua);
            LocalBox(marker,"Left",new Vector3(-w/2,.27f,0),new Vector3(.18f,.08f,d),aqua);
            LocalBox(marker,"Right",new Vector3(w/2,.27f,0),new Vector3(.18f,.08f,d),aqua);
            CombineStatic(marker,id+"-selection"); f.Highlight=marker.GetComponentInChildren<Renderer>(); f.Highlight.gameObject.SetActive(false);
            facilities.Add(f); return f;
        }
        static Mesh TankMesh()
        {
            const string path="Assets/_Game/Art/Meshes/HollowTank.asset";
            var existing=AssetDatabase.LoadAssetAtPath<Mesh>(path); if(existing) return existing;
            var vertices=new List<Vector3>(); var triangles=new List<int>(); const int n=48;
            for(int i=0;i<=n;i++)
            {
                float a=i*Mathf.PI*2/n; var radial=new Vector3(Mathf.Cos(a),0,Mathf.Sin(a));
                vertices.Add(radial*4); vertices.Add(radial*4+Vector3.up*5.5f); vertices.Add(radial*3.7f); vertices.Add(radial*3.7f+Vector3.up*5.5f);
            }
            for(int i=0;i<n;i++)
            {
                int v=i*4;
                foreach(var q in new[]{new[]{v,v+1,v+5,v+4},new[]{v+2,v+6,v+7,v+3},new[]{v+1,v+3,v+7,v+5}})
                    triangles.AddRange(new[]{q[0],q[1],q[2],q[0],q[2],q[3]});
            }
            var mesh=new Mesh{name="Hollow storage tank"}; mesh.SetVertices(vertices); mesh.SetTriangles(triangles,0); mesh.RecalculateNormals(); mesh.RecalculateBounds(); AssetDatabase.CreateAsset(mesh,path); return mesh;
        }
        static void Parking()
        {
            var parking=Utility("parking","Mobility Court","Parking, service access and a pedestrian corridor.",new Vector3(67,0,-30),42,23);
            parking.Capacity=100; parking.InitialOccupants=15;
            LocalBox(parking.transform,"Parking asphalt",new Vector3(0,.2f,0),new Vector3(41,.1f,22),road);
            var carPaints=new[]{
                Material("Car slate",new Color(.16f,.28f,.42f),.5f,.7f),
                Material("Car burgundy",new Color(.46f,.10f,.14f),.5f,.7f),
                Material("Car pearl",new Color(.88f,.89f,.86f),.2f,.8f),
                Material("Car forest",new Color(.11f,.26f,.15f),.4f,.65f),
                Material("Car charcoal",new Color(.15f,.17f,.19f),.6f,.75f),
                Material("Car amber",new Color(.65f,.43f,.12f),.5f,.7f)
            };
            for(int i=0;i<12;i++)
            {
                float x=-18+i*3.3f;
                LocalBox(parking.transform,"Bay line",new Vector3(x,.27f,5),new Vector3(.1f,.03f,8),white);
                if(i%3==0) continue;
                Car(parking.transform,new Vector3(x+1.5f,.2f,5),0,carPaints[i%carPaints.Length],i%2==1);
            }
        }
        static void Car(Transform parent,Vector3 p,float yaw,Material paint,bool isSuv=false)
        {
            var car=new GameObject(isSuv?"Parked SUV":"Parked Sedan").transform; car.SetParent(parent,false);
            car.localPosition=p; car.localRotation=Quaternion.Euler(0,yaw,0);
            float cabinH=isSuv?.7f:.55f;
            float cabinY=isSuv?.98f:.88f;
            float roofZ=isSuv?2.4f:2.1f;
            // Lower chassis & aerodynamic hood
            LocalBox(car,"Chassis",new Vector3(0,.35f,0),new Vector3(1.75f,.35f,3.9f),paint);
            LocalBox(car,"Hood slope",new Vector3(0,.42f,1.35f),new Vector3(1.68f,.25f,1.1f),paint);
            // Cabin superstructure
            LocalBox(car,"Cabin",new Vector3(0,cabinY,-.25f),new Vector3(1.48f,cabinH,roofZ),paint);
            // Windshield, rear window, side glass
            LocalBox(car,"Windshield",new Vector3(0,cabinY,.85f),new Vector3(1.38f,cabinH*.82f,.12f),glass);
            LocalBox(car,"Rear glass",new Vector3(0,cabinY,-1.32f),new Vector3(1.36f,cabinH*.78f,.12f),glass);
            LocalBox(car,"Side glass",new Vector3(0,cabinY,-.25f),new Vector3(1.52f,cabinH*.75f,1.7f),glass);
            // Bumpers & grille
            LocalBox(car,"Front bumper",new Vector3(0,.25f,1.98f),new Vector3(1.72f,.24f,.18f),metal);
            LocalBox(car,"Rear bumper",new Vector3(0,.25f,-1.98f),new Vector3(1.72f,.24f,.18f),metal);
            LocalBox(car,"Grille",new Vector3(0,.38f,1.98f),new Vector3(1.1f,.18f,.06f),metal);
            // Side view mirrors
            LocalBox(car,"Mirror L",new Vector3(-.94f,.75f,.75f),new Vector3(.18f,.12f,.15f),paint);
            LocalBox(car,"Mirror R",new Vector3(.94f,.75f,.75f),new Vector3(.18f,.12f,.15f),paint);
            // License plates
            LocalBox(car,"Plate F",new Vector3(0,.2f,2.08f),new Vector3(.55f,.14f,.03f),white);
            LocalBox(car,"Plate R",new Vector3(0,.2f,-2.08f),new Vector3(.55f,.14f,.03f),white);
            if(isSuv)
            {
                LocalBox(car,"Roof rail L",new Vector3(-.6f,1.38f,-.25f),new Vector3(.05f,.06f,2.1f),metal);
                LocalBox(car,"Roof rail R",new Vector3(.6f,1.38f,-.25f),new Vector3(.05f,.06f,2.1f),metal);
            }
            // Headlights (warm white glow) & taillights (red emissive)
            var lampGlow=Material("Headlight glow",new Color(1f,.96f,.85f));
            LocalBox(car,"Headlight L",new Vector3(-.62f,.38f,1.99f),new Vector3(.3f,.15f,.06f),lampGlow);
            LocalBox(car,"Headlight R",new Vector3(.62f,.38f,1.99f),new Vector3(.3f,.15f,.06f),lampGlow);
            var tailGlow=Material("Taillight glow",new Color(.92f,.12f,.08f));
            LocalBox(car,"Taillight L",new Vector3(-.62f,.4f,-1.99f),new Vector3(.3f,.14f,.06f),tailGlow);
            LocalBox(car,"Taillight R",new Vector3(.62f,.4f,-1.99f),new Vector3(.3f,.14f,.06f),tailGlow);
            // 4 Distinct rubber wheels with silver rims
            for(int x=-1;x<=1;x+=2) for(int z=-1;z<=1;z+=2)
            {
                var wheel=Shape(PrimitiveType.Cylinder,"Tire",car,new Vector3(x*.88f,.28f,z*1.18f),new Vector3(.58f,.16f,.58f),road);
                wheel.transform.localRotation=Quaternion.Euler(0,0,90);
                var hub=Shape(PrimitiveType.Cylinder,"Rim",car,new Vector3(x*.91f,.28f,z*1.18f),new Vector3(.36f,.18f,.36f),trim);
                hub.transform.localRotation=Quaternion.Euler(0,0,90);
            }
        }
        static GameObject LocalBox(Transform parent,string name,Vector3 p,Vector3 size,Material material)
        {
            var go=Box(name,parent.TransformPoint(p),size,material,parent); go.transform.localRotation=Quaternion.identity; Object.DestroyImmediate(go.GetComponent<Collider>()); return go;
        }
        static GameObject Shape(PrimitiveType kind,string name,Transform parent,Vector3 p,Vector3 size,Material material)
        {
            var go=GameObject.CreatePrimitive(kind); go.name=name; go.transform.SetParent(parent,false); go.transform.localPosition=p; go.transform.localScale=size;
            go.GetComponent<Renderer>().sharedMaterial=material; Object.DestroyImmediate(go.GetComponent<Collider>()); return go;
        }
        static void Sign(Transform parent,string caption,Vector3 p,float scale,Color color,bool world=false)
        {
            var go=new GameObject("Sign "+caption); go.transform.SetParent(parent,false);
            if(world) go.transform.position=p; else go.transform.localPosition=p;
            go.transform.localRotation=Quaternion.identity;
            var text=go.AddComponent<TextMesh>(); text.font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            go.GetComponent<MeshRenderer>().sharedMaterial=text.font.material;
            text.text=caption; text.fontSize=64; text.characterSize=scale*.4f; text.anchor=TextAnchor.MiddleCenter; text.color=color;
        }
        static void CombineStatic(Transform root,string id)
        {
            var groups=new Dictionary<Material,List<CombineInstance>>(); var originals=new List<GameObject>();
            foreach(var filter in root.GetComponentsInChildren<MeshFilter>())
            {
                var renderer=filter.GetComponent<MeshRenderer>(); if(!renderer || !filter.sharedMesh) continue;
                var mat=renderer.sharedMaterial; if(!groups.ContainsKey(mat)) groups[mat]=new List<CombineInstance>();
                groups[mat].Add(new CombineInstance{mesh=filter.sharedMesh,transform=root.worldToLocalMatrix*filter.transform.localToWorldMatrix}); originals.Add(filter.gameObject);
            }
            int index=0;
            foreach(var pair in groups)
            {
                var mesh=new Mesh{name=id+" "+pair.Key.name,indexFormat=IndexFormat.UInt32}; mesh.CombineMeshes(pair.Value.ToArray(),true,true); mesh.RecalculateBounds();
                string path="Assets/_Game/Art/Meshes/"+id+"-"+index+++".asset";
                var existing=AssetDatabase.LoadAssetAtPath<Mesh>(path);
                if(existing) { EditorUtility.CopySerialized(mesh,existing); Object.DestroyImmediate(mesh); mesh=existing; }
                else AssetDatabase.CreateAsset(mesh,path);
                var go=new GameObject(pair.Key.name,typeof(MeshFilter),typeof(MeshRenderer)); go.transform.SetParent(root,false);
                go.GetComponent<MeshFilter>().sharedMesh=mesh; go.GetComponent<MeshRenderer>().sharedMaterial=pair.Key;
                go.isStatic=!id.StartsWith("tree-crown");
            }
            foreach(var go in originals) Object.DestroyImmediate(go);
        }
    }
}
