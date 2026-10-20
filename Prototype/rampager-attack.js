(function(root){
  'use strict';
  class RampagerAttack {
    constructor(game,owner){this.game=game;this.owner=owner;this.attackInterval=1.5;this.contactRadius=.78;this.reset();}
    reset(){this.nextAttackAt=0;}
    get cooldownRemaining(){return Math.max(0,this.nextAttackAt-this.game.time);}
    tryAttack(allowed){const g=this.game;if(!allowed||g.dead||g.won||g.health<=0||g.time<this.nextAttackAt||!this.owner.perception.canTouch(this.contactRadius))return false;this.nextAttackAt=g.time+Math.max(.01,this.attackInterval);return g.takeMonsterHit();}
  }
  root.RampagerAttack=RampagerAttack;if(typeof module!=='undefined')module.exports=RampagerAttack;
})(globalThis);
