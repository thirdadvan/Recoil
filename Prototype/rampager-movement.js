(function(root){
  'use strict';
  class RampagerMovement {
    constructor(game,home,left,right){this.game=game;this.home={...home};this.left=left;this.right=right;this.patrolSpeed=.9;this.investigateSpeed=1.25;this.chaseSpeed=2.6;this.swarmAttackSpeedMultiplier=1.4;this.jumpHeightInBodies=.7;this.jumpCooldown=5;this.jumpGravity=14;this.jumpTriggerDistance=2.5;this.reset();}
    reset(){this.airborne=false;this.verticalSpeed=this.nextJumpAt=0;this.x=this.home.x;this.y=this.home.y;this.facing=-1;}
    get grounded(){return this.game.blocks.some(b=>this.x+.3>b.x&&this.x-.3<b.x+b.w&&Math.abs(this.y-.8-b.y-b.h)<.035);}
    get jumpCooldownRemaining(){return Math.max(0,this.nextJumpAt-this.game.time);}
    tryJump(target,blocked){if(this.airborne||this.game.time<this.nextJumpAt||!this.grounded||Math.abs(target.x-this.x)>this.jumpTriggerDistance||(!blocked&&target.y<=this.y+.35))return false;this.verticalSpeed=Math.sqrt(2*Math.max(.01,this.jumpGravity)*1.6*this.jumpHeightInBodies);this.airborne=true;this.nextJumpAt=this.game.time+Math.max(.01,this.jumpCooldown);return true;}
    simulateVertical(dt){if(!this.airborne||dt<=0)return;const amount=this.verticalSpeed*dt-.5*this.jumpGravity*dt*dt;this.verticalSpeed-=this.jumpGravity*dt;this.y+=amount;for(const b of this.game.blocks)if(this.x-.33<b.x+b.w&&this.x+.33>b.x&&this.y-.8<b.y+b.h&&this.y+.8>b.y){if(amount<=0){this.y=b.y+b.h+.8;this.airborne=false;}else this.y=b.y-.8;this.verticalSpeed=0;}}
    toward(x){return Math.abs(x-this.x)<.2?0:Math.sign(x-this.x);}
    lookAt(x){if(Math.abs(x-this.x)>.01)this.facing=Math.sign(x-this.x);}
    move(direction,distance,turnOnBlock){
      if(!direction||distance<=0)return false;
      this.facing=direction;const x=this.x+direction*distance;
      const blocked=this.game.blocks.some(b=>x-.33<b.x+b.w&&x+.33>b.x&&this.y-.8<b.y+b.h&&this.y+.8>b.y);
      if(blocked){if(turnOnBlock)this.facing*=-1;return false;}this.x=x;return true;
    }
  }
  root.RampagerMovement=RampagerMovement;if(typeof module!=='undefined')module.exports=RampagerMovement;
})(globalThis);
