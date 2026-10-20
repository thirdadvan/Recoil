(function(root){'use strict';const dist=(a,b)=>Math.hypot(a.x-b.x,a.y-b.y);class Stairway{
 constructor({points=[],travelSpeed=3.6,entryRadius=1.25,entryHeightTolerance=.3,catchRadius=.55}={}){Object.assign(this,{points,travelSpeed,entryRadius,entryHeightTolerance,catchRadius});this.length=points.slice(1).reduce((n,p,i)=>n+dist(points[i],p),0);}
 pointAt(d){d=Math.max(0,Math.min(this.length,d));for(let i=1;i<this.points.length;i++){const a=this.points[i-1],b=this.points[i],l=dist(a,b);if(d<=l||i===this.points.length-1){const t=l?d/l:0;return{x:a.x+(b.x-a.x)*t,y:a.y+(b.y-a.y)*t};}d-=l;}return this.points[0];}
 entry(feet,vertical){if(!this.length||Math.abs(vertical)<.1)return null;const d=vertical>0?0:this.length,p=this.pointAt(d);return Math.abs(feet.x-p.x)<=this.entryRadius&&Math.abs(feet.y-p.y)<=this.entryHeightTolerance?{stair:this,distance:d,approaching:true,heading:Math.sign(vertical)}:null;}
 catchAt(feet){let walked=0,best=null;for(let i=1;i<this.points.length;i++){const a=this.points[i-1],b=this.points[i],dx=b.x-a.x,dy=b.y-a.y,l=dist(a,b),t=l?Math.max(0,Math.min(1,((feet.x-a.x)*dx+(feet.y-a.y)*dy)/(l*l))):0,gap=dist(feet,{x:a.x+t*dx,y:a.y+t*dy});if(gap<=this.catchRadius&&(!best||gap<best.gap))best={stair:this,distance:walked+l*t,approaching:false,heading:1,gap};walked+=l;}return best;}
 advance(feet,vertical,multiplier,dt,state,horizontal=0){
  const step=this.travelSpeed*Math.max(0,multiplier)*dt;let start=0,end=0,index=1;state.heading=state.heading||1;
  for(;index<this.points.length;index++){end=start+dist(this.points[index-1],this.points[index]);if(state.distance<end-.0001||(state.distance<=end+.0001&&state.heading<0)||index===this.points.length-1)break;start=end;}
  const dx=this.points[index].x-this.points[index-1].x;let intent=Math.abs(vertical)>.1?Math.sign(vertical):Math.abs(horizontal)>.1?Math.sign(horizontal*dx):0;
  const turn=state.distance>.0001&&state.distance<this.length-.0001&&(Math.abs(state.distance-start)<.0001||Math.abs(state.distance-end)<.0001);
  if(Math.abs(vertical)<.1&&turn&&intent*state.heading<0)intent=0;
  if(state.approaching){if(!intent)return{position:feet,finished:false};if(state.distance===0&&intent<0||state.distance>0&&intent>0)return{position:feet,finished:true};state.heading=intent;const p=this.pointAt(state.distance),l=dist(feet,p),t=l?Math.min(1,step/l):1,position={x:feet.x+(p.x-feet.x)*t,y:feet.y+(p.y-feet.y)*t};if(dist(position,p)<.001)state.approaching=false;return{position,finished:false};}
  if(intent)state.heading=intent;let next=state.distance+intent*step;if(Math.abs(vertical)<.1&&intent)next=Math.max(start,Math.min(end,next));state.distance=Math.max(0,Math.min(this.length,next));return{position:this.pointAt(state.distance),finished:intent<0&&state.distance<=.0001||intent>0&&state.distance>=this.length-.0001};
 }
 }root.WalkableStairway=Stairway;if(typeof module!=='undefined')module.exports=Stairway;})(globalThis);
