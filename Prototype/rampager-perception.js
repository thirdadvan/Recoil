(function(root){
  'use strict';
  class RampagerPerception {
    constructor(game,owner){this.game=game;this.owner=owner;this.idealRange=32/3;this.fov=100;this.minimumExposure=.34;this.reset();}
    reset(){this.observedShot=this.observedInteraction=this.observedFall=0;}
    get sightRange(){return this.idealRange*this.game.illumination*(this.owner.state==='attack'?1.5:1);}
    canSee(){
      if(this.owner.state==='sleeping')return false;
      // An attacking monster cannot lose a touching target to its eye-cone blind spot.
      if(this.owner.state==='attack'&&this.canTouch(this.owner.attack.contactRadius))return true;
      const e=this.owner,g=this.game,eye={x:e.x,y:e.y+.45},dx=g.pos.x-eye.x,dy=g.pos.y-eye.y,n=Math.hypot(dx,dy);
      if(n>this.sightRange||(dx*e.sightDirection.x+dy*e.sightDirection.y)/Math.max(.001,n)<Math.cos(this.fov*.5*Math.PI/180))return false;
      let exposed=0;const half=g.crouched?.35:.65;for(let i=-1;i<=1;i++)if(g.clear(eye,{x:g.pos.x,y:g.pos.y+half*i}))exposed++;
      return exposed/3>=this.minimumExposure;
    }
    canLocate(){const e=this.owner,eye={x:e.x,y:e.y+.45};return Math.hypot(this.game.pos.x-eye.x,this.game.pos.y-eye.y)<=this.sightRange&&this.game.clear(eye,this.game.pos);}
    hearNoise(p,radius){if(Math.hypot(this.owner.x-p.x,this.owner.y-p.y)<=radius&&this.onNoise)this.onNoise(p);}
    canTouch(radius){const e=this.owner,g=this.game;return Math.hypot(g.pos.x-e.x,g.pos.y-e.y)<radius&&g.clear(e,g.pos);}
    sense(){
      const e=this.owner,g=this.game,sees=this.canSee(),v=g.movementVelocity||{x:0,y:0};
      const result={sees,playerMoving:Math.hypot(v.x,v.y)>.1,shot:sees&&g.time-g.shotAt<.18&&g.shotSequence!==this.observedShot,
        interaction:sees&&g.time<g.forbiddenUntil&&g.interactionSequence!==this.observedInteraction,highFall:sees&&g.highFall&&g.fallSequence!==this.observedFall};
      if(result.shot)this.observedShot=g.shotSequence;if(result.interaction)this.observedInteraction=g.interactionSequence;if(result.highFall)this.observedFall=g.fallSequence;
      if(this.onObserved)this.onObserved(result);return result;
    }
  }
  root.RampagerPerception=RampagerPerception;if(typeof module!=='undefined')module.exports=RampagerPerception;
})(globalThis);
