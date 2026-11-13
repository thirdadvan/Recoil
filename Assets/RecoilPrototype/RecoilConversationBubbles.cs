using System.Collections.Generic;
using UnityEngine;

public sealed partial class RecoilPrototype
{
    GUIStyle bubbleBody,bubbleHint,choiceBody,selectedChoiceBody,bubbleSurfaceStyle,thoughtSurfaceStyle;
    Font bubbleFont;
    Texture2D bubbleRound,bubbleTail,thoughtDot,thoughtRound;
    readonly List<Rect> bubbleMessageRects=new List<Rect>();
    readonly List<int> bubbleMessageIndices=new List<int>();
    readonly List<Rect> bubbleChoiceRects=new List<Rect>();
    Rect bubbleThoughtRect,bubbleReservedSpeechRect;
    public Rect ConversationReservedSpeechRect {get{return bubbleReservedSpeechRect;}}
    float thoughtMarkerY;
    float thoughtMotionStarted;
    Vector2 thoughtLayoutOrigin,thoughtLayoutCanvas;
    float thoughtLayoutDiameter,thoughtLayoutAngle;
    int thoughtMotionLine=-1;
    NpcDialogueSequence thoughtMotionSequence;
    int thoughtMarkerFrame=-1;
    bool thoughtMarkerVisible;
    public Rect ConversationThoughtRect {get{return bubbleThoughtRect;}}
    Vector2 bubblePlayerPoint,bubbleNpcPoint;
    float bubbleScale=1,bubbleCanvasWidth=1280,bubbleCanvasHeight=720;
    public Rect LatestConversationBubbleRect {get{return bubbleMessageRects.Count>0?bubbleMessageRects[bubbleMessageRects.Count-1]:new Rect();}}
    public Rect[] ConversationChoiceRects {get{return bubbleChoiceRects.ToArray();}}
    public Rect[] ConversationMessageRects {get{return bubbleMessageRects.ToArray();}}
    public int VisibleConversationMessageCount {get{return bubbleMessageRects.Count;}}
    public Vector2 ConversationPlayerScreenPoint {get{return bubblePlayerPoint;}}
    public Vector2 ConversationNpcScreenPoint {get{return bubbleNpcPoint;}}
    public float ConversationCanvasWidth {get{return bubbleCanvasWidth;}}
    public float ConversationCanvasHeight {get{return bubbleCanvasHeight;}}
    void OnDestroy()
    {
        CancelConversation();
        DisposeReadablePhoto();
        if(inventoryUI!=null)inventoryUI.Dispose();
        if(thoughtDot)Destroy(thoughtDot);if(thoughtRound)Destroy(thoughtRound);if(bubbleRound)Destroy(bubbleRound);if(bubbleTail)Destroy(bubbleTail);if(bubbleFont)Destroy(bubbleFont);
    }
    public void DrawConversationTalkPrompt(Transform anchor,string text)
    {
        if(!cam || !anchor)return;PrepareBubbleStyles();
        float scale=Mathf.Max(.1f,Mathf.Min(Screen.width/1280f,Screen.height/720f));
        var p=cam.WorldToScreenPoint(anchor.position);var matrix=GUI.matrix;
        GUI.matrix=Matrix4x4.Scale(new Vector3(scale,scale,1));
        var rect=new Rect(Mathf.Clamp(p.x/scale-120,24,Screen.width/scale-264),Mathf.Clamp((Screen.height-p.y)/scale-60,24,Screen.height/scale-80),240,38);
        BubbleSurface(rect,Hex(0x14252c,.94f));GUI.Label(rect,text,bubbleHint);GUI.matrix=matrix;
    }
    static void SetConversationTextColor(GUIStyle style,Color color)
    {
        style.normal.textColor=style.hover.textColor=style.active.textColor=style.focused.textColor=color;
        style.onNormal.textColor=style.onHover.textColor=style.onActive.textColor=style.onFocused.textColor=color;
    }
    void PrepareThoughtTextures()
    {
        if(thoughtDot && thoughtRound)return;
        thoughtDot=new Texture2D(64,64,TextureFormat.RGBA32,false){hideFlags=HideFlags.HideAndDontSave,wrapMode=TextureWrapMode.Clamp};
        thoughtRound=new Texture2D(256,256,TextureFormat.RGBA32,false){hideFlags=HideFlags.HideAndDontSave,wrapMode=TextureWrapMode.Clamp};
        for(int y=0;y<64;y++)for(int x=0;x<64;x++){
            thoughtDot.SetPixel(x,y,new Color(1,1,1,Mathf.Clamp01(31.5f-new Vector2(x-31.5f,y-31.5f).magnitude)));
        }
        for(int y=0;y<256;y++)for(int x=0;x<256;x++){
            float dx=x-127.5f,dy=y-127.5f,angle=Mathf.Atan2(dy,dx);
            // A round outline with a few gentle, asymmetric deviations.
            float radius=119*(1+.025f*Mathf.Sin(angle*3+.7f)+.014f*Mathf.Sin(angle*5+2.1f));
            float alpha=Mathf.Clamp01(radius-Mathf.Sqrt(dx*dx+dy*dy)+.5f);
            thoughtRound.SetPixel(x,y,new Color(1,1,1,alpha));
        }
        thoughtDot.Apply();thoughtRound.Apply();
        thoughtSurfaceStyle=new GUIStyle{normal={background=thoughtRound}};
    }
    void PrepareBubbleStyles()
    {
        PrepareThoughtTextures();
        if(bubbleBody!=null)return;
        bubbleFont=Font.CreateDynamicFontFromOSFont(new[]{"PingFang SC","Microsoft YaHei","Noto Sans CJK SC","Arial Unicode MS","Arial"},20);
        bubbleBody=new GUIStyle(GUI.skin.label){font=bubbleFont,fontSize=20,wordWrap=true,richText=false,padding=new RectOffset(),normal={textColor=RecoilDialogueBubblePalette.Surface(0x3C484B)}};
        bubbleHint=new GUIStyle(bubbleBody){fontSize=14,alignment=TextAnchor.MiddleCenter,normal={textColor=Hex(0xe2edf0)}};
        choiceBody=new GUIStyle(bubbleBody){fontSize=18};
        selectedChoiceBody=new GUIStyle(choiceBody);
        choiceBody.alignment=selectedChoiceBody.alignment=TextAnchor.MiddleCenter;
        SetConversationTextColor(bubbleBody,RecoilDialogueBubblePalette.Surface(0x3C484B));
        SetConversationTextColor(choiceBody,RecoilDialogueBubblePalette.Surface(0x606B69));
        SetConversationTextColor(selectedChoiceBody,RecoilDialogueBubblePalette.Surface(0x303C3D));
        SetConversationTextColor(bubbleHint,Hex(0xe2edf0));
        bubbleRound=new Texture2D(32,32,TextureFormat.RGBA32,false){hideFlags=HideFlags.HideAndDontSave,wrapMode=TextureWrapMode.Clamp};
        bubbleTail=new Texture2D(32,64,TextureFormat.RGBA32,false){hideFlags=HideFlags.HideAndDontSave,wrapMode=TextureWrapMode.Clamp};
        for(int y=0;y<32;y++)for(int x=0;x<32;x++){
            float dx=Mathf.Max(8-x,x-23),dy=Mathf.Max(8-y,y-23);
            bubbleRound.SetPixel(x,y,new Color(1,1,1,Mathf.Clamp01(8.5f-Mathf.Sqrt(Mathf.Pow(Mathf.Max(0,dx),2)+Mathf.Pow(Mathf.Max(0,dy),2)))));
        }
        for(int y=0;y<64;y++)for(int x=0;x<32;x++)bubbleTail.SetPixel(x,y,new Color(1,1,1,Mathf.Clamp01(y/63f*16-Mathf.Abs(x-15.5f))));
        bubbleRound.Apply();bubbleTail.Apply();
        bubbleSurfaceStyle=new GUIStyle{border=new RectOffset(8,8,8,8),normal={background=bubbleRound}};
    }
    Vector2 BubbleScreenPoint(Vector3 worldPoint)
    {
        var p=cam.WorldToScreenPoint(worldPoint);
        return new Vector2(p.x/bubbleScale,(Screen.height-p.y)/bubbleScale);
    }
    Vector3 PlayerConversationHead()
    {
        return player.WorldPosition+Vector3.up*(player.BodyHeight*.48f);
    }
    bool PlayerSpeaking(int index){return conversationLines[index].role==NpcDialogueSequence.SpeakerRole.Player;}
    static string ConversationChoiceText(NpcDialogueSequence.Choice choice){return string.IsNullOrWhiteSpace(choice.label)?choice.text??"":choice.label;}
    float ThoughtDiameter(NpcDialogueSequence.Choice[] choices)
    {
        float diameter=128;
        foreach(var choice in choices)foreach(var word in ConversationChoiceText(choice).Split(' '))
            diameter=Mathf.Max(diameter,choiceBody.CalcSize(new GUIContent(word)).x/.64f+8);
        return Mathf.Min(diameter,176);
    }
    const float ConversationSpeakerGap=-20; // A slight horizontal overlap between dialogue lanes at 720p.
    const float ConversationBubbleVerticalLift=48; // Reference pixels at 720p; scales with the rest of the dialogue UI.
    // Called from OnGUI and verification; every rectangle uses the same uniform screen scale.
    public void CalculateConversationBubbleLayout()
    {
        if(!ConversationActive || !conversationNpcAnchor || !cam || !player)return;
        PrepareBubbleStyles();
        bubbleScale=Mathf.Max(.1f,Mathf.Min(Screen.width/1280f,Screen.height/720f));
        bubbleCanvasWidth=Screen.width/bubbleScale;bubbleCanvasHeight=Screen.height/bubbleScale;
        bubblePlayerPoint=BubbleScreenPoint(PlayerConversationHead());bubbleNpcPoint=BubbleScreenPoint(conversationNpcAnchor.position);
        bubbleMessageRects.Clear();bubbleMessageIndices.Clear();bubbleChoiceRects.Clear();bubbleThoughtRect=bubbleReservedSpeechRect=new Rect();
        bool choosing=WaitingForConversationChoice;
        float margin=24,gap=ConversationSpeakerGap;
        float maxWidth=Mathf.Min(430,(bubbleCanvasWidth-margin*2-gap)*.5f);
        // Keep two stable lanes with a small overlap, even for nearby speakers.
        // Clamp the pair at screen edges so their relative spacing stays intact.
        float halfSpan=maxWidth+gap*.5f;
        float centre=Mathf.Clamp((bubblePlayerPoint.x+bubbleNpcPoint.x)*.5f,margin+halfSpan,bubbleCanvasWidth-margin-halfSpan);
        float leftLaneRight=centre-gap*.5f,rightLaneLeft=centre+gap*.5f;
        // Reserve Dawn's next line as soon as the choice-bearing sentence is
        // shown, before the menu opens. Its top thought temporarily uses that slot.
        bool reservedPlayer=conversationLine==conversationChoicePlayerLine;
        int choiceSource=ConversationHasChoices?conversationLine:reservedPlayer?conversationChoiceSourceLine:-1;
        float diameter=choiceSource>=0?ThoughtDiameter(conversationLines[choiceSource].choices):128;
        float slotHeight=diameter+36;
        if(choiceSource>=0)foreach(var choice in conversationLines[choiceSource].choices)
            slotHeight=Mathf.Max(slotHeight,28+bubbleBody.CalcHeight(new GUIContent(choice.text??""),maxWidth-32));
        float slotCentreY=Mathf.Max(margin+slotHeight*.5f,bubblePlayerPoint.y-1.55f*diameter);
        if(choiceSource>=0){
            float speechX=conversationPlayerOnLeft?bubblePlayerPoint.x+diameter*.5f+8-maxWidth:bubblePlayerPoint.x-diameter*.5f-8;
            bubbleReservedSpeechRect=new Rect(Mathf.Clamp(speechX,margin,bubbleCanvasWidth-margin-maxWidth),slotCentreY-slotHeight*.5f,maxWidth,slotHeight);
        }
        // Showing choices must not remove history or change any dialogue rectangle.
        int first=Mathf.Max(0,conversationLine-2);
        var heights=new List<float>();var widths=new List<float>();var messageIndices=new List<int>();float total=0;
        for(int i=first;i<=conversationLine;i++){
            if(ConversationChoicePrompt(i))continue;
            messageIndices.Add(i);
            string text=conversationLines[i].text??"";
            float width=Mathf.Clamp(bubbleBody.CalcSize(new GUIContent(text)).x+36,150,maxWidth);
            float height=28+bubbleBody.CalcHeight(new GUIContent(text),width-32);
            widths.Add(width);heights.Add(height);
            if(!reservedPlayer || i!=conversationLine)total+=height+10;
        }
        float bottom=Mathf.Clamp(Mathf.Min(bubblePlayerPoint.y,bubbleNpcPoint.y)-40-ConversationBubbleVerticalLift,170,bubbleCanvasHeight-64);
        if(choiceSource>=0){
            // Keep at least the current NPC sentence on screen when headroom is tight.
            float minimum=heights.Count==0?24:24+(reservedPlayer && heights.Count>1?heights[heights.Count-2]:heights[heights.Count-1])+10;
            bottom=Mathf.Max(minimum,Mathf.Min(bottom,bubbleReservedSpeechRect.yMin-16));
        }
        while(heights.Count>1 && total>bottom-24){total-=heights[0]+10;heights.RemoveAt(0);widths.RemoveAt(0);messageIndices.RemoveAt(0);}
        float y=Mathf.Max(24,bottom-total);
        for(int j=0;j<heights.Count;j++){
            int i=messageIndices[j];bool left=PlayerSpeaking(i)?conversationPlayerOnLeft:!conversationPlayerOnLeft;
            float x=left?leftLaneRight-widths[j]:rightLaneLeft;
            if(reservedPlayer && i==conversationLine)x=conversationPlayerOnLeft?bubbleReservedSpeechRect.xMax-widths[j]:bubbleReservedSpeechRect.xMin;
            float messageY=reservedPlayer && i==conversationLine?slotCentreY-heights[j]*.5f:y;
            bubbleMessageRects.Add(new Rect(x,messageY,widths[j],heights[j]));bubbleMessageIndices.Add(i);
            if(!reservedPlayer || i!=conversationLine)y+=heights[j]+10;
        }
        if(!choosing)return;
        var choices=conversationLines[conversationLine].choices;

        bool newThought=thoughtMotionSequence!=conversation || thoughtMotionLine!=conversationLine;
        bool reflow=newThought || thoughtLayoutCanvas!=new Vector2(bubbleCanvasWidth,bubbleCanvasHeight) || !Mathf.Approximately(thoughtLayoutDiameter,diameter);
        if(newThought){thoughtMotionSequence=conversation;thoughtMotionLine=conversationLine;thoughtMotionStarted=Time.unscaledTime;thoughtLayoutAngle=0;}
        var localThoughts=new List<Rect>();float side=conversationPlayerOnLeft?-1:1;
        for(int i=0;i<choices.Length;i++){
            // Reference sketch: one above Dawn, then two staggered down the
            // outer side. Mirror the arrangement when the NPC is on the left.
            float angle=(i*50+thoughtLayoutAngle)*Mathf.Deg2Rad;
            Vector2 offset=new Vector2(side*Mathf.Sin(angle),-Mathf.Cos(angle))*(1.55f*diameter);
            float size=diameter*(i==1?.96f:i>=2?1.08f:1);
            localThoughts.Add(new Rect(offset.x-size*.5f,offset.y-size*.5f,size,size));
        }
        Vector2 preferred=bubblePlayerPoint;
        preferred.y=slotCentreY+1.55f*diameter;
        if(reflow){thoughtLayoutOrigin=PlaceConversationThoughts(localThoughts,diameter,margin,preferred);thoughtLayoutCanvas=new Vector2(bubbleCanvasWidth,bubbleCanvasHeight);thoughtLayoutDiameter=diameter;}
        Vector2 origin=thoughtLayoutOrigin;
        for(int i=0;i<localThoughts.Count;i++){
            var r=localThoughts[i];r.position+=origin+ThoughtDrift(i);bubbleChoiceRects.Add(r);
            bubbleThoughtRect=i==0?r:Rect.MinMaxRect(Mathf.Min(bubbleThoughtRect.xMin,r.xMin),Mathf.Min(bubbleThoughtRect.yMin,r.yMin),Mathf.Max(bubbleThoughtRect.xMax,r.xMax),Mathf.Max(bubbleThoughtRect.yMax,r.yMax));
        }
    }
    Vector2 ThoughtDrift(int index)
    {
        float time=Time.unscaledTime-thoughtMotionStarted,seed=13.7f+index*19.31f;
        // Smooth noise rather than frame-by-frame random jitter. These same
        // moving rectangles are used by rendering and mouse hit testing.
        return new Vector2((Mathf.PerlinNoise(seed,time*.18f+4.61f)-.5f)*8,(Mathf.PerlinNoise(seed+31.4f,time*.16f+19.27f)-.5f)*6);
    }
    Vector2 PlaceConversationThoughts(List<Rect> thoughts,float diameter,float margin,Vector2 preferred)
    {
        var feet=BubbleScreenPoint(player.WorldPosition-Vector3.up*(player.BodyHeight*.5f));
        var playerBounds=Rect.MinMaxRect(bubblePlayerPoint.x-26,bubblePlayerPoint.y-18,bubblePlayerPoint.x+26,Mathf.Max(bubblePlayerPoint.y+40,feet.y)+12);
        float outer=conversationPlayerOnLeft?-1:1;
        // Prefer a tiny angular adjustment over translating the group: this
        // keeps every centre on the same radius around Dawn.
        for(float turn=0;turn<=20;turn+=5){
            var proposed=new List<Rect>();bool clear=true;
            for(int i=0;i<thoughts.Count;i++){
                float angle=(i*50+turn)*Mathf.Deg2Rad;
                var local=thoughts[i];local.center=new Vector2(outer*Mathf.Sin(angle),-Mathf.Cos(angle))*(1.55f*diameter);
                var rect=local;rect.position+=preferred;
                if(rect.xMin<margin || rect.xMax>bubbleCanvasWidth-margin || rect.yMin<margin || rect.yMax>bubbleCanvasHeight-60 || rect.Overlaps(playerBounds)){clear=false;break;}
                foreach(var message in bubbleMessageRects)if(rect.Overlaps(new Rect(message.x-6,message.y-6,message.width+12,message.height+12))){clear=false;break;}
                if(!clear)break;proposed.Add(local);
            }
            if(clear){thoughtLayoutAngle=turn;thoughts.Clear();thoughts.AddRange(proposed);return preferred;}
        }
        var bounds=thoughts[0];foreach(var r in thoughts)bounds=Rect.MinMaxRect(Mathf.Min(bounds.xMin,r.xMin),Mathf.Min(bounds.yMin,r.yMin),Mathf.Max(bounds.xMax,r.xMax),Mathf.Max(bounds.yMax,r.yMax));
        float minX=margin-bounds.xMin,maxX=bubbleCanvasWidth-margin-bounds.xMax;
        float minY=margin-bounds.yMin,maxY=bubbleCanvasHeight-60-bounds.yMax;
        Vector2 fallback=new Vector2(Mathf.Clamp(preferred.x,minX,maxX),Mathf.Clamp(preferred.y,minY,maxY));
        float side=conversationPlayerOnLeft?-1:1;
        var xs=new List<float>{fallback.x,Mathf.Clamp(preferred.x+side*diameter*.5f,minX,maxX),Mathf.Clamp(preferred.x+side*diameter,minX,maxX),minX,maxX};
        var ys=new List<float>{fallback.y,minY,maxY};
        foreach(var message in bubbleMessageRects)foreach(var thought in thoughts){ys.Add(message.yMin-thought.yMax-12);ys.Add(message.yMax-thought.yMin+12);}
        Vector2 best=fallback;float bestCost=float.PositiveInfinity;
        foreach(float x in xs)foreach(float y in ys){
            if(y<minY || y>maxY)continue;
            bool blocked=false;float sidePenalty=0;
            for(int i=0;i<thoughts.Count;i++){
                var rect=thoughts[i];rect.position+=new Vector2(x,y);
                if(rect.Overlaps(playerBounds)){blocked=true;break;}
                foreach(var message in bubbleMessageRects)if(rect.Overlaps(new Rect(message.x-6,message.y-6,message.width+12,message.height+12))){blocked=true;break;}
                if(blocked)break;
                if(i>0 && (rect.center.x-bubblePlayerPoint.x)*side<24)sidePenalty+=500;
            }
            if(blocked)continue;
            float cost=Mathf.Abs(x-preferred.x)*1.5f+Mathf.Abs(y-preferred.y)+sidePenalty;
            if(cost<bestCost){bestCost=cost;best=new Vector2(x,y);}
        }
        return best;
    }
    void BubbleSurface(Rect rect,Color color)
    {
        GUI.color=color;GUI.Box(rect,GUIContent.none,bubbleSurfaceStyle);GUI.color=Color.white;
    }
    void BubbleArrow(Rect rect,Vector2 target,Color color)
    {
        // Bury the whole base inside the surface, then let the bubble cover it.
        // A rotated base at the edge can leave a gap when pointing diagonally.
        if(rect.Contains(target))return;
        const float rootInset=10;
        Vector2 start;
        if(target.y>=rect.yMax)start=new Vector2(Mathf.Clamp(target.x,rect.xMin+20,rect.xMax-20),rect.yMax-rootInset);
        else if(target.y<=rect.yMin)start=new Vector2(Mathf.Clamp(target.x,rect.xMin+20,rect.xMax-20),rect.yMin+rootInset);
        else start=new Vector2(target.x<rect.xMin?rect.xMin+rootInset:rect.xMax-rootInset,Mathf.Clamp(target.y,rect.yMin+12,rect.yMax-12));
        Vector2 delta=target-start;float distance=delta.magnitude;if(distance<5)return;
        Vector2 direction=delta/distance;
        // Measure the actual ray exit, including shallow angles that leave via
        // a neighbouring edge, so the visible part has a consistent length.
        float exitX=Mathf.Abs(direction.x)<.0001f?float.PositiveInfinity:((direction.x>0?rect.xMax:rect.xMin)-start.x)/direction.x;
        float exitY=Mathf.Abs(direction.y)<.0001f?float.PositiveInfinity:((direction.y>0?rect.yMax:rect.yMin)-start.y)/direction.y;
        float exitDistance=Mathf.Min(exitX,exitY);
        float visibleLength=Mathf.Lerp(14,32,Mathf.InverseLerp(120,400,distance));
        float length=Mathf.Min(distance-8,exitDistance+visibleLength);
        if(length<=exitDistance)return;
        var matrix=GUI.matrix;
        GUI.matrix=matrix*Matrix4x4.TRS(start,Quaternion.Euler(0,0,Mathf.Atan2(delta.y,delta.x)*Mathf.Rad2Deg-90),Vector3.one)*Matrix4x4.Translate(-start);
        GUI.color=color;GUI.DrawTexture(new Rect(start.x-7,start.y,14,length),bubbleTail);GUI.color=Color.white;GUI.matrix=matrix;
    }
    string VisibleConversationText()
    {
        string text=CurrentConversationText??"";if(ConversationTextComplete)return text;
        var elements=System.Globalization.StringInfo.ParseCombiningCharacters(text);
        int count=Mathf.Clamp(Mathf.FloorToInt((Time.unscaledTime-conversationLineStarted)*30),0,elements.Length);
        return count==elements.Length?text:text.Substring(0,elements[count]);
    }
    Color NpcConversationBubbleFill(int index)
    {return RecoilDialogueBubblePalette.NpcFill(conversationLines[index].speaker??"",gameObject.scene.name);}
    static bool ThoughtContains(Rect rect,Vector2 point)
    {return new Vector2((point.x-rect.center.x)/rect.width,(point.y-rect.center.y)/rect.height).sqrMagnitude<.48f*.48f;}
    void DrawConversationThoughts()
    {
        if(bubbleChoiceRects.Count==0){thoughtMarkerVisible=false;return;}
        // Each short intention has its own thought bubble. Only the selected
        // thought carries the connector, keeping the separate choices uncluttered.
        Color background=RecoilDialogueBubblePalette.Surface(0xF2F4EF);
        var selectedRect=bubbleChoiceRects[Mathf.Clamp(selectedConversationChoice,0,bubbleChoiceRects.Count-1)];
        Vector2 direction=bubblePlayerPoint-selectedRect.center;
        if(direction.magnitude>selectedRect.width*.5f+30){
            direction.Normalize();GUI.color=background;
            Vector2 attachment=selectedRect.center+direction*(selectedRect.width*.46f);
            var large=attachment+direction*12;var small=attachment+direction*25;
            GUI.DrawTexture(new Rect(large.x-5,large.y-5,10,10),thoughtDot);
            GUI.DrawTexture(new Rect(small.x-3,small.y-3,6,6),thoughtDot);GUI.color=Color.white;
        }
        float target=bubbleChoiceRects[Mathf.Clamp(selectedConversationChoice,0,bubbleChoiceRects.Count-1)].center.y;
        if(!thoughtMarkerVisible){thoughtMarkerY=target;thoughtMarkerVisible=true;}
        if(thoughtMarkerFrame!=Time.frameCount){thoughtMarkerY=Mathf.Lerp(thoughtMarkerY,target,1-Mathf.Exp(-22*Time.unscaledDeltaTime));thoughtMarkerFrame=Time.frameCount;}
        for(int i=0;i<bubbleChoiceRects.Count;i++){
            var r=bubbleChoiceRects[i];bool active=i==selectedConversationChoice;var c=conversationLines[conversationLine].choices[i];
            GUI.color=RecoilDialogueBubblePalette.Surface(active?0xF2F4EF:0xD6DCD5);
            var matrix=GUI.matrix;
            GUI.matrix=matrix*Matrix4x4.TRS(r.center,Quaternion.Euler(0,0,i*47-13),Vector3.one)*Matrix4x4.Translate(-r.center);
            GUI.DrawTexture(r,thoughtRound);GUI.matrix=matrix;GUI.color=Color.white;
            GUI.Label(new Rect(r.x+r.width*.17f,r.y+r.height*.25f,r.width*.66f,r.height*.5f),ConversationChoiceText(c),active?selectedChoiceBody:choiceBody);
            if(Event.current.type==EventType.MouseDown && Event.current.button==0 && ThoughtContains(r,Event.current.mousePosition) && conversationInputFrame!=Time.frameCount){SelectConversationChoice(i);Event.current.Use();break;}
        }
        GUI.Label(new Rect(selectedRect.x+selectedRect.width*.07f,thoughtMarkerY-12,16,24),"›",selectedChoiceBody);
    }
    void DrawConversationBubbles()
    {
        CalculateConversationBubbleLayout();var matrix=GUI.matrix;
        GUI.matrix=Matrix4x4.Scale(new Vector3(bubbleScale,bubbleScale,1));
        for(int j=0;j<bubbleMessageRects.Count;j++){
            int index=bubbleMessageIndices[j];bool current=index==conversationLine,isPlayer=PlayerSpeaking(index);
            Color fill=isPlayer?Hex(0xc9eaf6):NpcConversationBubbleFill(index);
            if(!current)fill=Color.Lerp(fill,isPlayer?Hex(0xb3bdc0):fill*.84f,.2f);
            var r=bubbleMessageRects[j];
            if(current)BubbleArrow(r,isPlayer?bubblePlayerPoint:bubbleNpcPoint,fill);
            BubbleSurface(r,fill);
            GUI.Label(new Rect(r.x+16,r.y+14,r.width-32,r.height-28),current?VisibleConversationText():conversationLines[index].text,bubbleBody);
        }
        DrawConversationThoughts();
        string hint=WaitingForConversationChoice?(RecoilInput.UsingGamepad?"D-pad Select · × Confirm":"↑ ↓ Select · Enter / Space Confirm"):
            (RecoilInput.UsingGamepad?"× Continue":"Space / Enter / Click to continue");
        float hintY=bubbleCanvasHeight-42;
        if(bubbleChoiceRects.Count>0)hintY=Mathf.Min(hintY,bubbleChoiceRects[bubbleChoiceRects.Count-1].yMax+8);
        GUI.Label(new Rect(24,hintY,bubbleCanvasWidth-48,30),hint,bubbleHint);GUI.matrix=matrix;
    }
}
