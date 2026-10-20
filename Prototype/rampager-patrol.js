(function(root){
  'use strict';
  // Coordinates are fixed world positions, matching Unity's captured child behaviour points.
  class RampagerPatrolRoute {
    constructor(points=[],loop=true){this.points=points.map((p,index)=>({...p,index})).sort((a,b)=>a.order-b.order||a.index-b.index);this.loop=loop;this.reset();}
    reset(){this.index=0;this.complete=false;this.lastFacing=-1;this.suspend();}
    suspend(){this.elapsed=0;this.arrived=false;this.entryFacing=-1;}
    get current(){return this.points[this.index];}
    advance(){if(this.index+1<this.points.length)this.index++;else if(this.loop)this.index=0;else this.complete=true;}
    evaluate(x,dt,facing,speed){
      if(!this.points.length)return null;
      if(this.complete)return {direction:0,distance:0,facing:this.lastFacing};
      for(let skipped=0;skipped<this.points.length;skipped++){
        const point=this.current,delta=point.x-x;
        if(Math.abs(delta)>.025)return {direction:Math.sign(delta),distance:Math.min(Math.abs(delta),speed*dt),facing:Math.sign(delta)};
        if(!this.arrived){this.arrived=true;this.entryFacing=facing;}
        const duration=Math.max(0,point.duration),actionDt=Math.min(dt,Math.max(0,duration-this.elapsed));this.elapsed+=actionDt;
        let look=this.entryFacing;
        if(point.action==='lookLeft')look=-1;
        else if(point.action==='lookRight')look=1;
        else if(point.action==='lookAround')look=Math.floor(this.elapsed/Math.max(.05,point.lookInterval||.75))%2===0?-1:1;
        this.lastFacing=look;
        if(this.elapsed<duration)return {direction:0,distance:0,facing:look};
        this.advance();this.suspend();
        if(duration>0||this.complete)return {direction:0,distance:0,facing:look};
      }
      return {direction:0,distance:0,facing};
    }
    static example(home,left,right){return new RampagerPatrolRoute([
      {order:0,x:left,duration:1.5,action:'lookRight'},
      {order:1,x:home,duration:3,action:'lookAround',lookInterval:.75},
      {order:2,x:right,duration:1.5,action:'lookLeft'},
      {order:3,x:home,duration:1.5,action:'stay'}
    ]);}
  }
  root.RampagerPatrolRoute=RampagerPatrolRoute;if(typeof module!=='undefined')module.exports=RampagerPatrolRoute;
})(globalThis);
