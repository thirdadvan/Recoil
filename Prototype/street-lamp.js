(function(root){
  'use strict';
  class StreetLamp {
    constructor({x=22,y=4.8,coneLength=4.8,coneHalfWidth=2,interactionRange=1.2,initiallyOn=true,unstableStreetLamp=false,brightDuration={x:.5,y:1.5},darkDuration={x:2.5,y:3.5},direction={x:0,y:-1}}={}){Object.assign(this,{x,y,coneLength,coneHalfWidth,interactionRange,initiallyOn,unstableStreetLamp,brightDuration,darkDuration,direction});this.reset();}
    reset(){this.switchedOn=this.initiallyOn;this.flickerLit=true;this.phase=0;this.remaining=this.phaseDuration(0);}
    get isOn(){return this.switchedOn&&(!this.unstableStreetLamp||this.flickerLit);}
    set isOn(value){this.switchedOn=value;}
    phaseDuration(phase){const range=phase===0?this.brightDuration:phase===5?this.darkDuration:null;return range?range.x+Math.random()*(range.y-range.x):phase===2||phase===4?.06+Math.random()*.06:.08+Math.random()*.12;}
    tick(dt){if(!this.switchedOn||!this.unstableStreetLamp)return;this.remaining-=dt;while(this.remaining<=0){this.phase=(this.phase+1)%6;this.remaining+=this.phaseDuration(this.phase);this.flickerLit=[0,2,4].includes(this.phase);}}

    contains(p){const dx=p.x-this.x,dy=p.y-this.y,depth=dx*this.direction.x+dy*this.direction.y,side=Math.abs(dx*this.direction.y-dy*this.direction.x);return this.isOn&&depth>=0&&depth<=this.coneLength&&side<=this.coneHalfWidth*depth/Math.max(.1,this.coneLength);}
    inRange(g){return this.switchedOn&&Math.hypot(g.pos.x-this.x,g.pos.y-this.y)<=this.interactionRange&&g.clear(g.pos,this);}
    interact(g){if(!this.inRange(g))return false;if(g.activeItem)g.activeItem.cancel();g.activeItem=null;g.interactionHeld=true;g.notifyInteraction();this.isOn=false;g.lighting.sampleKey=null;g.showInteractionMessage('路灯已关闭。');return true;}
  }
  root.StreetLamp=StreetLamp;if(typeof module!=='undefined')module.exports=StreetLamp;
})(globalThis);
