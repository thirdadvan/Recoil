using System;
using System.Collections.Generic;
using UnityEngine;

// An overlay over the existing inventory flags, not a second gameplay inventory.
// Coordinates are authored at 1600 x 900 and uniformly letterboxed on other displays.
public sealed class RecoilInventoryUI : IDisposable
{
    public sealed class Item
    {
        public string id,name,description,kind;public int width,height,cell;public Texture2D icon;
        public Item(string id,string name,string description,string kind,int w=1,int h=1)
        {this.id=id;this.name=name;this.description=description;this.kind=kind;width=w;height=h;}
    }
    readonly RecoilPrototype world;
    readonly List<Item> items=new List<Item>();
    readonly int[] slots=new int[9];
    Texture2D coat,atlas,outline,motif,glasses,equippedBacking;
    Item pickup;bool pickupArmed;int pickupFrame;
    public bool IsPickup {get{return pickup!=null;}}
    public const float PickupShadeOpacity=.78f;
    Rect gunUV,keyUV,fuseUV;
    GUIStyle text,heading,small,center,button,title,pickupBody;
    static Font font;
    readonly Color ink=new Color(.88f,.87f,.82f),line=new Color(.19f,.18f,.16f),amber=new Color(1,.59f,.08f);
    int selected,menuIndex;
    bool preview,previewEquipped=true,actions,inspect;
    float openedAt,oldTimeScale,nextMove;
    CursorLockMode oldLock;bool oldVisible;
    public bool IsOpen {get;private set;}
    public bool Preview {get{return preview;}}
    public int SelectedCell {get{return selected;}}
    public int ItemCount {get{return items.Count;}}
    public int SlotOwner(int cell){return slots[cell];}
    public Item SelectedItem {get{return selected>=0&&selected<9&&slots[selected]>=0?items[slots[selected]]:null;}}
    public float SelectionBrightness {get{return Mathf.Lerp(.45f,1,.5f+.5f*Mathf.Cos((Time.unscaledTime-openedAt)*Mathf.PI*2));}}
    public const float BackgroundShadeOpacity=.15f;
    const float Cell=148,Step=168;
    static Rect CellRect(int cell){return new Rect(265+(cell%3)*Step,260+(cell/3)*Step,Cell,Cell);}
    public RecoilInventoryUI(RecoilPrototype context){world=context;}
    public void Open(bool sampleItems)
    {
        if(IsOpen)return;
        pickup=null;preview=sampleItems;previewEquipped=true;BuildItems();selected=items.Count>0?items[0].cell:0;
        actions=inspect=false;menuIndex=0;openedAt=Time.unscaledTime;nextMove=0;
        LoadAssets();oldTimeScale=Time.timeScale;oldLock=Cursor.lockState;oldVisible=Cursor.visible;
        IsOpen=true;Time.timeScale=0;Cursor.lockState=CursorLockMode.None;Cursor.visible=true;
    }
    public static Item Describe(string id,string itemName=null,string description=null,Texture2D icon=null)
    {
        Item item;
        switch(id)
        {
            case "water_gun":item=new Item(id,"WATER GUN","A retro water blaster with a detachable tank.\nFire, then use the direction controls to fly.","gun",2,1);break;
            case "fuse":case "capacitor":case "warehouse_fuse":item=new Item(id,"CERAMIC FUSE","A ceramic cartridge fuse with metal end caps.","fuse",1,2);break;
            case "warehouse_locked_room_key":item=new Item(id,"STOREROOM KEY","The manager's key to the warehouse storeroom.","key");break;
            case "debug_universal_door_key":item=new Item(id,"TEST KEY","Opens every locked door.","key");break;
            case "employee_access_card":item=new Item(id,"EMPLOYEE ACCESS CARD","An employee access card for secured entrances.","card");break;
            case "winding_key":item=new Item(id,"WINDING KEY","A clockwork winding key. Used to wind a stopped mechanical clock.","key");item.icon=Resources.Load<Texture2D>("RecoilInventory/WindingKey");break;
            case "crowbar":item=new Item(id,"BANDAGED CROWBAR","A heavy steel pry bar with a bandage-wrapped grip.\nUsed to pry open stuck covers.","crowbar",1,2);item.icon=Resources.Load<Texture2D>("RecoilInventory/Crowbar");break;
            case ChildFoodQuest.Sandwich:item=new Item(id,"WARM SANDWICH","A freshly heated sandwich from The Counter Sales. Good food for a hungry child.","sandwich");break;
            case ChildFoodQuest.Biscuits:item=new Item(id,"DAMP BISCUITS","A few slightly damp biscuits from The Disguised Man. Not much, but edible.","biscuits");break;
            case "manager_glasses":item=new Item(id,"MANAGER'S GLASSES","A pair of dark brown square-framed glasses belonging to the factory manager.","glasses");break;
            default:item=new Item(id,string.IsNullOrEmpty(itemName)?id.Replace('_',' ').ToUpperInvariant():itemName.ToUpperInvariant(),description??"","unknown");break;
        }
        if(icon)item.icon=icon;return item;
    }
    public void OpenPickup(Item item,bool sample=false)
    {Open(sample);pickup=item;pickupFrame=Time.frameCount;pickupArmed=false;}
    public void Close()
    {if(!IsOpen)return;IsOpen=false;Time.timeScale=oldTimeScale;Cursor.lockState=oldLock;Cursor.visible=oldVisible;}
    public void Dispose(){Close();if(outline)UnityEngine.Object.Destroy(outline);if(motif)UnityEngine.Object.Destroy(motif);if(equippedBacking)UnityEngine.Object.Destroy(equippedBacking);}
    void BuildItems()
    {
        items.Clear();for(int i=0;i<9;i++)slots[i]=-1;
        if(preview||world.OwnsWaterGun)Add(new Item("water_gun","WATER GUN","A retro water blaster with a detachable tank.\nFire, then use the direction controls to fly.","gun",2,1));
        if(preview||world.HasItem("fuse")||world.HasItem("capacitor")||world.HasItem("warehouse_fuse"))Add(new Item("fuse","CERAMIC FUSE","A ceramic cartridge fuse with metal end caps.","fuse",1,2));
        if(preview)Add(new Item("preview_key","BRASS KEY","A small brass key.","key"));
        else
        {
            if(world.HasItem("warehouse_locked_room_key"))Add(new Item("warehouse_locked_room_key","STOREROOM KEY","The manager's key to the warehouse storeroom. Used automatically at the matching door.","key"));
            if(world.HasItem(RecoilPrototype.UniversalKey))Add(new Item(RecoilPrototype.UniversalKey,"TEST KEY","Bypasses item, clue, story and power requirements when interacting with objects.","key"));
            if(world.HasItem("employee_access_card"))Add(new Item("employee_access_card","EMPLOYEE ACCESS CARD","An employee access card. Used automatically at secured entrances.","card"));
            if(world.HasItem(WindUpArchiveClock.KeyId))Add(Describe(WindUpArchiveClock.KeyId));
            if(world.HasItem(CrowbarPickup.InventoryId))Add(Describe(CrowbarPickup.InventoryId));
            foreach(var food in new[]{ChildFoodQuest.Sandwich,ChildFoodQuest.Biscuits})if(world.HasItem(food))Add(Describe(food));
            if(world.HasItem(ManagerGlassesPickup.InventoryId))Add(new Item(ManagerGlassesPickup.InventoryId,"MANAGER'S GLASSES","The manager's dark brown square glasses.\nFound on the warehouse roof.","glasses"));
        }
    }
    bool Fits(int cell,int w,int h)
    {
        if(cell%3+w>3||cell/3+h>3)return false;
        for(int y=0;y<h;y++)for(int x=0;x<w;x++)if(slots[cell+y*3+x]>=0)return false;return true;
    }
    void Add(Item item)
    {
        for(int c=0;c<9;c++)if(Fits(c,item.width,item.height))
        {item.cell=c;int index=items.Count;items.Add(item);for(int y=0;y<item.height;y++)for(int x=0;x<item.width;x++)slots[c+y*3+x]=index;return;}
    }
    void LoadAssets()
    {
        if(coat)return;
        coat=Resources.Load<Texture2D>("RecoilInventory/Coat");glasses=Resources.Load<Texture2D>("RecoilInventory/ManagerGlasses");atlas=Resources.Load<Texture2D>("RecoilInventory/Items");
        glasses=Resources.Load<Texture2D>("RecoilInventory/ManagerGlasses");
        gunUV=Trim(new Rect(0,.49f,1,.51f));keyUV=Trim(new Rect(0,0,.55f,.44f));fuseUV=Trim(new Rect(.61f,0,.39f,.47f));
        outline=new Texture2D(160,160,TextureFormat.RGBA32,false);outline.name="Inventory rounded slot outline";
        var pixels=new Color[160*160];
        for(int y=0;y<160;y++)for(int x=0;x<160;x++)
        {
            float qx=Mathf.Abs(x-79.5f)-72,qy=Mathf.Abs(y-79.5f)-72;
            float d=Mathf.Sqrt(Mathf.Max(qx,0)*Mathf.Max(qx,0)+Mathf.Max(qy,0)*Mathf.Max(qy,0))+Mathf.Min(Mathf.Max(qx,qy),0)-6;
            pixels[y*160+x]=new Color(1,1,1,Mathf.Clamp01(1.8f-Mathf.Abs(d)));
        }
        outline.SetPixels(pixels);outline.Apply(false,true);outline.wrapMode=TextureWrapMode.Clamp;
        // Top-right backing is inset and rounded to stay inside the slot contour.
        equippedBacking=new Texture2D(100,28,TextureFormat.RGBA32,false);
        equippedBacking.name="Equipped corner backing";
        var badgePixels=new Color[100*28];
        for(int y=0;y<28;y++)for(int x=0;x<100;x++)
        {
            float top=27-y;bool inside=x>=2&&x<98&&top>=2&&top<26;
            float dx=x-90,dy=top-10;
            float alpha=inside?1:0;
            if(x>90&&top<10)alpha*=Mathf.Clamp01(8-Mathf.Sqrt(dx*dx+dy*dy));
            badgePixels[y*100+x]=new Color(.025f,.026f,.026f,alpha);
        }
        equippedBacking.SetPixels(badgePixels);equippedBacking.Apply(false,true);equippedBacking.wrapMode=TextureWrapMode.Clamp;
        motif=new Texture2D(96,96,TextureFormat.RGBA32,false);motif.name="Empty slot diagonal marks";
        var marks=new Color[96*96];
        for(int y=0;y<96;y++)for(int x=0;x<96;x++)
        {
            float px=x-47.5f,py=y-47.5f;float extent=Mathf.Max(Mathf.Abs(px),Mathf.Abs(py));
            bool stroke=extent>16&&extent<39&&Mathf.Abs(Mathf.Abs(px)-Mathf.Abs(py))<1;
            float alpha=stroke||px*px+py*py<6?1:0;marks[y*96+x]=new Color(1,1,1,alpha);
        }
        motif.SetPixels(marks);motif.Apply(false,true);motif.wrapMode=TextureWrapMode.Clamp;
        if(!font)font=Font.CreateDynamicFontFromOSFont(new[]{"Arial Narrow","Helvetica Neue","Arial"},30);
        text=Style(20,TextAnchor.UpperLeft);text.wordWrap=true;pickupBody=new GUIStyle(text){alignment=TextAnchor.UpperCenter};heading=Style(39,TextAnchor.MiddleLeft,FontStyle.Bold);
        title=Style(48,TextAnchor.MiddleLeft,FontStyle.Bold);title.fontStyle=FontStyle.BoldAndItalic;
        small=Style(16,TextAnchor.MiddleLeft);center=Style(22,TextAnchor.MiddleCenter);button=Style(26,TextAnchor.MiddleCenter,FontStyle.Bold);
    }
    // Tight alpha bounds are used as texture coordinates; source artwork is left untouched.
    Rect Trim(Rect uv)
    {
        if(!atlas)return uv;
        try
        {
            int x0=Mathf.RoundToInt(uv.x*atlas.width),y0=Mathf.RoundToInt(uv.y*atlas.height);
            int w=Mathf.RoundToInt(uv.width*atlas.width),h=Mathf.RoundToInt(uv.height*atlas.height);
            var pixels=atlas.GetPixels(x0,y0,w,h);int minX=w,minY=h,maxX=-1,maxY=-1;
            for(int y=0;y<h;y++)for(int x=0;x<w;x++)if(pixels[y*w+x].a>.1f){minX=Mathf.Min(minX,x);minY=Mathf.Min(minY,y);maxX=Mathf.Max(maxX,x);maxY=Mathf.Max(maxY,y);}
            if(maxX>=0)return new Rect((x0+minX)/(float)atlas.width,(y0+minY)/(float)atlas.height,(maxX-minX+1)/(float)atlas.width,(maxY-minY+1)/(float)atlas.height);
        }catch(UnityException){}return uv;
    }
    GUIStyle Style(int size,TextAnchor align,FontStyle weight=FontStyle.Normal)
    {var s=new GUIStyle{font=font,fontSize=size,alignment=align,fontStyle=weight};s.normal.textColor=ink;return s;}
    bool Equipped(Item item){return item!=null&&item.kind=="gun"&&(preview?previewEquipped:world.WaterGunEquipped);}
    public void MoveSelection(int dx,int dy)
    {
        int current=slots[selected];
        int c=selected%3,r=selected/3;
        while(true)
        {
            c+=dx;r+=dy;if(c<0||c>2||r<0||r>2)return;
            int n=r*3+c;if(current>=0&&slots[n]==current)continue;
            selected=n;openedAt=Time.unscaledTime;actions=inspect=false;return;
        }
    }
    void Select(int cell){selected=cell;actions=inspect=false;menuIndex=0;openedAt=Time.unscaledTime;}
    public void ToggleEquipment()
    {if(SelectedItem==null||SelectedItem.kind!="gun")return;if(preview)previewEquipped=!previewEquipped;else world.SetWaterGunEquipped(!world.WaterGunEquipped);}
    public void Tick()
    {
        if(IsPickup)
        {
            bool held=Input.GetMouseButton(0)||Input.GetKey(KeyCode.Return)||Input.GetKey(KeyCode.Space)||Input.GetKey(KeyCode.E)||RecoilInput.ConfirmHeld||RecoilInput.InteractHeld;
            if(Time.frameCount>pickupFrame&&!held)pickupArmed=true;
            if(pickupArmed&&(Input.GetKeyDown(KeyCode.Return)||Input.GetKeyDown(KeyCode.Space)||RecoilInput.ConfirmPressed||RecoilInput.InteractPressed||RecoilInput.PausePressed))world.CloseInventory();
            return;
        }
        if(RecoilInput.FilesPressed){world.OpenInformationJournal();return;}
        if(RecoilInput.InventoryPressed){world.CloseInventory();return;}
        if(RecoilInput.PausePressed||RecoilInput.InteractPressed)
        {if(inspect)inspect=false;else if(actions)actions=false;else world.CloseInventory();return;}
        bool confirm=Input.GetKeyDown(KeyCode.Return)||RecoilInput.ConfirmPressed;
        Vector2 pad=RecoilInput.PadMovement;
        int dx=Input.GetKeyDown(KeyCode.D)||Input.GetKeyDown(KeyCode.RightArrow)?1:Input.GetKeyDown(KeyCode.A)||Input.GetKeyDown(KeyCode.LeftArrow)?-1:0;
        int dy=Input.GetKeyDown(KeyCode.S)||Input.GetKeyDown(KeyCode.DownArrow)?1:Input.GetKeyDown(KeyCode.W)||Input.GetKeyDown(KeyCode.UpArrow)?-1:0;
        if(pad.sqrMagnitude>.25f&&Time.unscaledTime>=nextMove){if(Mathf.Abs(pad.x)>Mathf.Abs(pad.y))dx=pad.x>0?1:-1;else dy=pad.y>0?-1:1;nextMove=Time.unscaledTime+.22f;}
        if(pad.sqrMagnitude<.1f)nextMove=0;
        if(inspect){if(confirm)inspect=false;return;}
        if(actions){if(dy!=0)menuIndex=(menuIndex+dy+ActionCount)%ActionCount;if(dx<0)actions=false;if(confirm)Perform(menuIndex);}
        else{if(dx!=0||dy!=0)MoveSelection(dx,dy);if(confirm&&SelectedItem!=null){actions=true;menuIndex=0;}}
    }
    int ActionCount {get{return SelectedItem!=null&&SelectedItem.kind=="gun"?2:1;}}
    void Perform(int action)
    {if(SelectedItem==null)return;if(SelectedItem.kind=="gun"&&action==0)ToggleEquipment();else inspect=true;}
    void Label(Rect rect,string value,GUIStyle style,Color? color=null)
    {var previous=GUI.color;GUI.color=color??ink;GUI.Label(rect,value,style);GUI.color=previous;}
    static void Fill(Rect r,Color color){var old=GUI.color;GUI.color=color;GUI.DrawTexture(r,Texture2D.whiteTexture);GUI.color=old;}
    static void Rule(Rect r,Color color){Fill(r,color);}
    void Border(Rect r,Color color,float width=1.6f)
    {
        if(Mathf.Abs(r.width-r.height)>2){Rule(new Rect(r.x,r.y,r.width,1.3f),color);Rule(new Rect(r.x,r.yMax-1.3f,r.width,1.3f),color);Rule(new Rect(r.x,r.y,1.3f,r.height),color);Rule(new Rect(r.xMax-1.3f,r.y,1.3f,r.height),color);return;}
        var old=GUI.color;GUI.color=color;GUI.DrawTexture(r,outline);GUI.color=old;
    }
    static Rect Inset(Rect r,float padding){return new Rect(r.x+padding,r.y+padding,r.width-padding*2,r.height-padding*2);}
    Rect Footprint(Item item){var r=CellRect(item.cell);return new Rect(r.x,r.y,(item.width-1)*Step+Cell,(item.height-1)*Step+Cell);}
    void Picture(Rect target,string kind)
    {
        if(kind=="glasses")
        {if(glasses)GUI.DrawTexture(target,glasses,ScaleMode.ScaleToFit,true);return;}
        if(kind=="card")
        {var r=Inset(target,20);Fill(r,new Color(.7f,.68f,.58f));Label(Inset(r,10),"ACCESS CARD",small,new Color(.12f,.13f,.12f));return;}
        if(kind=="sandwich"||kind=="biscuits"){Label(target,kind=="sandwich"?"SANDWICH":"BISCUITS",center);return;}
        if(kind=="unknown"){Label(target,"ITEM",center);return;}
        if(!atlas)return;Rect uv=kind=="gun"?gunUV:kind=="fuse"?fuseUV:keyUV;
        float aspect=uv.width*atlas.width/(uv.height*atlas.height);float w=target.width,h=w/aspect;if(h>target.height){h=target.height;w=h*aspect;}
        GUI.DrawTextureWithTexCoords(new Rect(target.center.x-w*.5f,target.center.y-h*.5f,w,h),atlas,uv,true);
    }
    void ItemPicture(Rect target,Item item)
    {if(item.icon)GUI.DrawTexture(target,item.icon,ScaleMode.ScaleToFit,true);else Picture(target,item.kind);}
    void DrawPickup()
    {
        // The paused camera remains visible: this overlay never draws the coat background.
        Label(new Rect(300,100,1000,40),"ITEM ACQUIRED",center,new Color(.57f,.57f,.54f));
        ItemPicture(new Rect(420,195,760,310),pickup);
        Label(new Rect(300,550,1000,65),pickup.name,button);
        Rule(new Rect(610,626,380,1),new Color(.24f,.24f,.22f));
        Label(new Rect(400,658,800,95),pickup.description,pickupBody);
        var confirm=new Rect(620,785,360,48);Border(confirm,new Color(.3f,.3f,.28f));
        Label(confirm,RecoilInput.UsingGamepad?"CROSS / CONTINUE":"ENTER / CONTINUE",center);
        if(pickupArmed&&Click(confirm))world.CloseInventory();
    }
    void Motif(Rect r)
    {var old=GUI.color;GUI.color=new Color(.065f,.065f,.065f);GUI.DrawTexture(Inset(r,29),motif);GUI.color=old;}
    bool Click(Rect r)
    {if(Event.current.type==EventType.MouseDown&&Event.current.button==0&&r.Contains(Event.current.mousePosition)){Event.current.Use();return true;}return false;}
    public void Draw()
    {
        if(!IsOpen)return;LoadAssets();var oldMatrix=GUI.matrix;var oldColor=GUI.color;var oldEnabled=GUI.enabled;int oldDepth=GUI.depth;
        GUI.depth=-200;GUI.matrix=Matrix4x4.identity;GUI.color=Color.white;GUI.enabled=true;
        try
        {
            if(!IsPickup)Fill(new Rect(0,0,Screen.width,Screen.height),Color.black);
            else Fill(new Rect(0,0,Screen.width,Screen.height),new Color(0,0,0,PickupShadeOpacity));
            float scale=Mathf.Min(Screen.width/1600f,Screen.height/900f);
            GUI.matrix=Matrix4x4.TRS(new Vector3((Screen.width-1600*scale)*.5f,(Screen.height-900*scale)*.5f,0),Quaternion.identity,Vector3.one*scale);
            if(IsPickup){DrawPickup();return;}
            if(coat){GUI.color=new Color(.22f,.22f,.22f,1);GUI.DrawTexture(new Rect(0,0,1600,900),coat,ScaleMode.StretchToFill);GUI.color=Color.white;}
            // Background-only shade; all slots, items and labels are drawn afterwards.
            Fill(new Rect(0,0,1600,900),new Color(0,0,0,BackgroundShadeOpacity));
            Label(new Rect(140,24,260,65),"RECOIL",title);Label(new Rect(342,39,60,42),"///",button,amber);
            Fill(new Rect(520,32,235,44),new Color(.88f,.49f,.025f));Label(new Rect(520,32,235,44),"INVENTORY",button,new Color(.035f,.035f,.025f));
            Label(new Rect(790,30,135,50),"/   FILES",center,new Color(.68f,.68f,.62f));if(Click(new Rect(785,25,180,60))){world.OpenInformationJournal();return;}Label(new Rect(995,30,135,50),"/   MAP",center,new Color(.38f,.38f,.34f));
            int owner=slots[selected];float pulse=SelectionBrightness;
            for(int c=0;c<9;c++)
            {
                var rect=CellRect(c);bool active=owner>=0?slots[c]==owner:c==selected;
                Color edge=active?Color.Lerp(line,Color.white,pulse):line;Border(rect,edge);
                if(slots[c]<0)Motif(rect);
                if(Click(rect))Select(c);
            }
            foreach(var item in items)ItemPicture(Inset(Footprint(item),17),item);
            foreach(var item in items)if(Equipped(item))
            {
                var cell=CellRect(item.cell+item.width-1);var tag=new Rect(cell.xMax-100,cell.y,100,28);
                GUI.DrawTexture(tag,equippedBacking);
                Color edge=owner>=0&&slots[item.cell]==owner?Color.Lerp(line,Color.white,pulse):line;
                // Only the inner dividers belong to the badge. The outer edges belong to the slot.
                Rule(new Rect(tag.x,tag.y+2,1.5f,tag.height-2),edge);
                Rule(new Rect(tag.x,tag.yMax-1.5f,tag.width-2,1.5f),edge);
                Label(Inset(tag,2),"EQUIPPED",small);
                // Repaint only the corner above the badge, preserving icon-over-grid layering elsewhere.
                GUI.BeginGroup(tag);
                Border(new Rect(cell.x-tag.x,cell.y-tag.y,cell.width,cell.height),edge);
                GUI.EndGroup();
            }
            DrawDetails();
            Label(new Rect(74,826,1300,35),RecoilInput.UsingGamepad?"LEFT STICK / SELECT     CROSS / CONFIRM     CIRCLE / BACK     R1 / FILES     TOUCHPAD / CLOSE":"WASD / SELECT     ENTER / CONFIRM     ESC / BACK     TAB / FILES     I / CLOSE",small);
            if(preview)Label(new Rect(80,785,650,28),"PREVIEW — sample items; game progress is unchanged",small,new Color(.42f,.42f,.39f));
            if(inspect)DrawInspect();
        }
        finally{GUI.matrix=oldMatrix;GUI.color=oldColor;GUI.enabled=oldEnabled;GUI.depth=oldDepth;}
    }
    void DrawDetails()
    {
        var item=SelectedItem;
        Label(new Rect(1005,130,470,65),item!=null?item.name:"EMPTY SLOT",heading);
        Rule(new Rect(1005,195,465,1),line);
        if(item==null){Label(new Rect(1005,470,440,90),"Select an item to inspect or equip it.",text);return;}
        Border(new Rect(1410,137,86,40),line);Label(new Rect(1410,137,86,40),item.width+" × "+item.height,center);
        ItemPicture(new Rect(1010,225,450,235),item);
        Label(new Rect(1005,482,470,100),item.description,text);Rule(new Rect(1005,581,465,1),line);
        string[] labels=item.kind=="gun"?new[]{Equipped(item)?"UNEQUIP":"EQUIP","INSPECT"}:new[]{"INSPECT"};
        for(int i=0;i<labels.Length;i++)
        {
            Rect r=new Rect(1005,609+i*69,465,53);bool focus=actions&&menuIndex==i;
            if(focus)Fill(Inset(r,3),new Color(.2f,.13f,.035f,.85f));Border(r,focus?amber:line);Label(r,labels[i],button,focus?amber:ink);
            if(Click(r)){actions=true;menuIndex=i;Perform(i);}
        }
    }
    void DrawInspect()
    {
        Fill(new Rect(0,0,1600,900),new Color(.012f,.015f,.018f,.97f));var item=SelectedItem;if(item==null)return;
        Label(new Rect(130,60,1250,70),item.name,heading);ItemPicture(new Rect(200,180,1200,480),item);
        Label(new Rect(300,700,1000,70),item.description,text);Label(new Rect(300,810,1000,35),"ESC / CIRCLE / BACK",center);
        if(Click(new Rect(1250,55,210,55)))inspect=false;Label(new Rect(1250,55,210,55),"BACK",button);
    }
}
