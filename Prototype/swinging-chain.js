(function(root){
  'use strict';
  const clamp=(v,a,b)=>Math.max(a,Math.min(b,v));
  class SwingingChain {
    constructor(def){Object.assign(this,{length:3.8,climbSpeed:2.4,grabRadius:.22,gravity:9.81,damping:.18,inputAcceleration:7,shotImpulse:4,maximumAngle:80,initialAngle:5,linkSpacing:.18},def);this.angle=this.initialAngle*Math.PI/180;this.omega=0;this.grip=this.length;this.occupied=false;}
    get direction(){return {x:Math.sin(this.angle),y:-Math.cos(this.angle)};}
    get position(){const d=this.direction;return {x:this.x+d.x*this.grip,y:this.y+d.y*this.grip};}
    get velocity(){return {x:Math.cos(this.angle)*this.grip*this.omega,y:Math.sin(this.angle)*this.grip*this.omega};}
    contains(p,height){if(this.occupied)return false;const d=this.direction;for(const n of [-1,0,1]){const s={x:p.x,y:p.y+height*.4*n};const r=clamp((s.x-this.x)*d.x+(s.y-this.y)*d.y,.9,this.length);if(Math.hypot(s.x-this.x-d.x*r,s.y-this.y-d.y*r)<=this.grabRadius+.32)return true;}return false;}
    attach(p){const d=this.direction;this.grip=clamp((p.x-this.x)*d.x+(p.y-this.y)*d.y,.95,this.length);this.occupied=true;}
    integrate(dt,r,a,rdot=0){this.omega+=(-this.gravity/r*Math.sin(this.angle)+a/r*Math.cos(this.angle)-(this.damping+2*rdot/r)*this.omega)*dt;this.angle+=this.omega*dt;const limit=this.maximumAngle*Math.PI/180;if(Math.abs(this.angle)>limit){this.angle=clamp(this.angle,-limit,limit);this.omega*=-.15;}}
    drive(x,y,dt,speed=1){const steps=Math.max(1,Math.ceil(dt*120)),h=dt/steps;for(let n=0;n<steps;n++){const old=this.grip;this.grip=clamp(old-y*this.climbSpeed*speed*h,.95,this.length);this.integrate(h,this.grip,x*this.inputAcceleration*speed,(this.grip-old)/Math.max(.00001,h));}}
    idle(dt){if(this.occupied)return;const steps=Math.max(1,Math.ceil(dt*120));for(let n=0;n<steps;n++)this.integrate(dt/steps,this.length,0);}
    shot(aim){this.omega+=(-aim.x*Math.cos(this.angle)-aim.y*Math.sin(this.angle))*this.shotImpulse/Math.max(.95,this.grip);}
    stopAt(p){this.angle=clamp(Math.atan2(p.x-this.x,this.y-p.y),-this.maximumAngle*Math.PI/180,this.maximumAngle*Math.PI/180);this.omega=0;this.grip=clamp(Math.hypot(p.x-this.x,p.y-this.y),.95,this.length);}
  }
  root.SwingingChain=SwingingChain;if(typeof module!=='undefined')module.exports=SwingingChain;
})(globalThis);
