(function(root){
  'use strict';
  const Attack=typeof module!=='undefined'?require('./rampager-attack.js'):root.RampagerAttack;
  const Motor=typeof module!=='undefined'?require('./rampager-movement.js'):root.RampagerMovement;
  let reportSequence=0;
  const Senses=typeof module!=='undefined'?require('./rampager-perception.js'):root.RampagerPerception;
  class RampagerAI {
    constructor(game,x,left,right){
      this.game=game;this.home={x,y:.8};this.sleepPosition={...this.home};this.movement=new Motor(game,this.home,left,right);this.perception=new Senses(game,this);this.attack=new Attack(game,this);
      this.initialState='roaming';this.spawnFacing=-1;this.returnToSleepAfterSound=true;this.soundInspectionDuration=1.5;this.sleepDelay=10;this.wakeDuration=1;
      this.roamingSightPitch=-25;this.waveSightPitch=15;this.nearDetectionDistance=1.5;this.nearAttackTime=.3;this.farAttackTime=3;this.attackLostTime=20;this.attackSpeedMultiplier=1.5;this.highFlightStopBodies=1.5;this.stoppedTolerance=.5;this.reverseTolerance=.5;this.reportRadius=10;this.reportFreshness=3;this.searchTime=6;
      this.perception.onNoise=p=>this.onNoiseHeard(p);this.reset();
    }
    get x(){return this.movement.x;}set x(v){this.movement.x=v;}
    get y(){return this.movement.y;}set y(v){this.movement.y=v;}
    get facing(){return this.movement.facing;}set facing(v){this.movement.facing=v;}
    get left(){return this.movement.left;}get right(){return this.movement.right;}
    get idealRange(){return this.perception.idealRange;}set idealRange(v){this.perception.idealRange=v;}
    get fov(){return this.perception.fov;}set fov(v){this.perception.fov=v;}
    get minimumExposure(){return this.perception.minimumExposure;}set minimumExposure(v){this.perception.minimumExposure=v;}
    get attackInterval(){return this.attack.attackInterval;}set attackInterval(v){this.attack.attackInterval=v;}
    get sightRange(){return this.perception.sightRange;}
    get watchingPlayer(){return this.watching&&this.state==='roaming'&&!this.waveActive;}
    get redEyes(){return this.waveActive||this.swarmAttackBoost;}
    get swarmAttackBoost(){return this.state==='attack'&&this.attackWaveBoost;}
    get alert(){return this.state==='attack'?1:this.detectionProgress;}
    get investigate(){return this.inspectionRemaining;}
    get sightPitch(){return this.watchingPlayer||this.state==='attack'||(!this.waveActive&&this.inspectionRemaining>0)?this.trackedPitch:this.waveActive?this.waveSightPitch:this.roamingSightPitch;}
    get sightDirection(){const a=this.sightPitch*Math.PI/180;return {x:this.facing*Math.cos(a),y:Math.sin(a)};}
    get currentAttackDelay(){const d=Math.hypot(this.x-this.game.pos.x,this.y+.45-this.game.pos.y),t=Math.max(0,Math.min(1,(d-this.nearDetectionDistance)/Math.max(.1,this.sightRange-this.nearDetectionDistance)));return this.nearAttackTime+(this.farAttackTime-this.nearAttackTime)*t;}
    get currentMoveSpeed(){return this.state==='attack'?this.movement.chaseSpeed*this.attackSpeedMultiplier*(this.swarmAttackBoost?this.movement.swarmAttackSpeedMultiplier:1):this.movement.patrolSpeed;}
    get waveMoving(){return this.waveActive&&this.waveDelay<=0&&this.waveMove>0;}
    reset(){
      this.waking=false;this.wakeRemaining=0;this.lastHeardPosition={x:0,y:0};this.stopped=this.reverse=0;this.waveAction=-1;this.reports=new Set();this.lastReportAt=-100;this.escapeSweep=0;this.movement.reset();this.perception.reset();this.attack.reset();if(this.patrol)this.patrol.reset();this.state=this.initialState;this.facing=this.spawnFacing;
      if(this.state==='sleeping'){this.x=this.sleepPosition.x;this.y=this.sleepPosition.y;}
      this.target={x:this.x,y:this.y};this.lastSight=-100;this.inspectionRemaining=this.sleepRemaining=this.detectionProgress=this.stun=this.waveDelay=this.waveMove=this.waveHold=this.waveRecovery=this.lastWaveId=this.waveIndex=0;
      this.watching=this.returning=this.waitingToSleep=this.waveActive=this.attackWaveBoost=this.sleepingBeforeWave=false;this.trackedPitch=this.roamingSightPitch;this.waveDirection=0;
      if(this.state==='attack'){this.target={...this.game.pos};this.lastSight=this.game.time;}
    }
    canSee(){return this.state!=='sleeping'&&this.perception.canSee();}
    hearNoise(p,radius){this.perception.hearNoise(p,radius);}
    cancelReturn(){this.returning=this.waitingToSleep=false;this.sleepRemaining=0;}
    setState(state){
      if(this.state===state)return;if(this.patrol)this.patrol.suspend();this.state=state;this.waking=false;this.wakeRemaining=0;this.stopped=this.reverse=0;this.waveAction=-1;this.escapeSweep=0;this.watching=false;this.detectionProgress=this.inspectionRemaining=0;this.cancelReturn();
      if(state!=='attack')this.attackWaveBoost=false;
      if(state==='sleeping'){this.sleepingBeforeWave=false;this.waveActive=false;this.waveDelay=this.waveMove=this.waveHold=this.waveRecovery=0;this.facing=this.spawnFacing;}
      this.trackedPitch=this.waveActive?this.waveSightPitch:this.roamingSightPitch;
    }
    trackPlayer(){this.movement.lookAt(this.game.pos.x);this.trackedPitch=Math.atan2(this.game.pos.y-(this.y+.45),Math.abs(this.game.pos.x-this.x))*180/Math.PI;}
    enterAttack(){const boost=this.waveActive||this.attackWaveBoost;this.setState('attack');this.attackWaveBoost=boost;this.target={...this.game.pos};this.lastSight=this.game.time;this.trackPlayer();}
    onNoiseHeard(p){
      if(this.state==='attack'||this.waveActive||this.watchingPlayer)return;
      if(this.state==='sleeping'){this.lastHeardPosition={x:p.x,y:p.y};if(!this.waking){this.waking=true;this.wakeRemaining=Math.max(.01,this.wakeDuration);}return;}
      this.setState('roaming');this.cancelReturn();this.movement.lookAt(p.x);this.trackedPitch=this.roamingSightPitch;
      if(this.patrol)this.patrol.suspend();this.inspectionRemaining=this.soundInspectionDuration;
    }
    directHit(p){this.stun=.65;this.enterAttack();this.target={...p};this.lastSight=this.game.time;}
    receive(report){
      const g=this.game;if(!report.attack||!g.enemies.includes(report.source)||report.source===this||report.target!==g||report.sightTime>g.time||g.time-report.sightTime>this.reportFreshness||this.reports.has(report.id)||report.sightTime<this.lastSight)return false;
      this.reports.add(report.id);if(this.state==='sleeping'){this.onNoiseHeard(report.source);return true;}this.enterAttack();this.target={...report.position};this.lastSight=report.sightTime;return true;
    }
    reportSight(sees){const g=this.game;if(this.state!=='attack'||!sees||g.time-this.lastReportAt<.5)return;this.lastReportAt=g.time;g.publishReport({source:this,target:g,position:{...this.target},sightTime:this.lastSight,attack:true,id:++reportSequence,eventType:'confirmed'},this.reportRadius);}
    finishSoundInspection(){this.inspectionRemaining=0;if(this.returnToSleepAfterSound){this.returning=true;this.waitingToSleep=false;this.sleepRemaining=0;if(this.patrol)this.patrol.suspend();}}
    receiveWarning(duration){
      if(!this.waveActive)this.sleepingBeforeWave=this.state==='sleeping';
      if(this.state==='sleeping')this.setState('roaming');this.waveActive=true;if(this.state==='attack')this.attackWaveBoost=true;
      this.watching=false;this.detectionProgress=this.inspectionRemaining=0;this.cancelReturn();this.trackedPitch=this.waveSightPitch;
      this.stopped=this.reverse=0;this.waveAction=0;this.waveDelay=this.waveMove=this.waveRecovery=this.waveIndex=0;this.waveHold=Math.max(0,duration);if(this.patrol)this.patrol.suspend();return true;
    }
    receiveWave(id,index,total,direction){
      if(id<=this.lastWaveId)return false;if(!this.waveActive)this.receiveWarning(0);this.lastWaveId=id;this.waveIndex=index;this.waveDirection=direction<0?-1:1;
      this.stopped=this.reverse=0;this.waveAction=-1;const sw=this.game.soundWaves;this.waveDelay=sw.moveDelay;this.waveMove=sw.commandDuration;this.waveHold=sw.holdDuration;this.waveRecovery=0;if(this.state!=='attack')this.facing=this.waveDirection;return true;
    }
    endSwarm(duration){if(this.waveActive)this.waveRecovery=Math.max(.01,duration);}
    advanceWave(dt){
      if(!this.waveActive)return {delay:0,move:0,hold:0};
      if(this.waveRecovery>0){this.waveRecovery=Math.max(0,this.waveRecovery-dt);if(this.waveRecovery<=0){const restoreSleep=this.sleepingBeforeWave;this.sleepingBeforeWave=false;this.stopped=this.reverse=0;this.waveAction=-1;this.waveActive=false;this.waveDelay=this.waveMove=this.waveHold=0;if(restoreSleep)this.setState('sleeping');else if(this.state==='roaming')this.finishSoundInspection();}return {delay:0,move:0,hold:0};}
      const delay=Math.min(dt,this.waveDelay);this.waveDelay-=delay;const move=Math.min(dt-delay,this.waveMove);this.waveMove-=move;const hold=dt-delay-move;this.waveHold=Math.max(0,this.waveHold-hold);return {delay,move,hold};
    }
    checkWaveIntent(o,dt,moving){
      if(dt<=1e-6||this.state==='attack')return;const action=moving?1:0;
      if(this.waveAction!==action){this.stopped=this.reverse=0;this.waveAction=action;}
      const g=this.game;if(!o.sees||g.movementBlocked){this.stopped=this.reverse=0;return;}
      const intent=g.horizontalIntent;
      this.stopped=moving&&Math.abs(intent)<.1?this.stopped+dt:0;
      this.reverse=(moving?intent*this.waveDirection<0:Math.abs(intent)>.1)?this.reverse+dt:0;
      if(this.stopped>=this.stoppedTolerance-1e-6||this.reverse>=this.reverseTolerance-1e-6)this.enterAttack();
    }
    tick(dt){
      if(dt<=0)return;const g=this.game;this.stun=Math.max(0,this.stun-dt);const wave=this.advanceWave(dt);
      if(this.state==='sleeping'){this.movement.simulateVertical(dt);if(this.waking){this.wakeRemaining=Math.max(0,this.wakeRemaining-dt);if(this.wakeRemaining<=1e-6){const p=this.lastHeardPosition;this.setState('roaming');this.movement.lookAt(p.x);this.trackedPitch=Math.atan2(p.y-(this.y+.45),Math.abs(p.x-this.x))*180/Math.PI;this.inspectionRemaining=this.soundInspectionDuration;if(this.canSee())this.enterAttack();}}return;}
      if((this.watchingPlayer||this.state==='attack')&&this.perception.canLocate())this.trackPlayer();
      const o=this.perception.sense();
      if(o.sees){this.target={...g.pos};this.lastSight=g.time;this.escapeSweep=0;}
      if(this.state==='attack'){this.reportSight(o.sees);if(!o.sees&&g.time-this.lastSight>=this.attackLostTime){this.setState('roaming');this.inspectionRemaining=this.searchTime;}}
      else if(this.state!=='attack'&&this.waveActive){if(o.sees&&(g.violating||o.shot||o.interaction||o.highFall))this.enterAttack();else{this.checkWaveIntent(o,wave.delay,false);this.checkWaveIntent(o,wave.move,true);this.checkWaveIntent(o,wave.hold,false);}}
      else if(o.sees){this.cancelReturn();this.watching=true;if(this.patrol)this.patrol.suspend();this.trackPlayer();this.detectionProgress=Math.min(1,this.detectionProgress+dt/Math.max(.01,this.currentAttackDelay));if(this.detectionProgress>=1-1e-6)this.enterAttack();}
      else{this.watching=false;this.detectionProgress=0;if(this.inspectionRemaining<=0)this.trackedPitch=this.roamingSightPitch;if(this.inspectionRemaining>0){this.inspectionRemaining=Math.max(0,this.inspectionRemaining-dt);if(this.inspectionRemaining<=0)this.finishSoundInspection();}}
      let direction=0,distance=0,turn=false;
      this.watchingHighFlyer=this.state==='attack'&&o.sees&&g.flying&&(g.pos.y-g.height/2-(this.y-.8)>1.65*this.highFlightStopBodies);
      if(this.watchingHighFlyer){this.facing=g.pos.x>=this.x?1:-1;this.trackPlayer();}
      if(this.state==='attack'&&!this.watchingHighFlyer){direction=this.movement.toward(this.target.x);let speed=this.currentMoveSpeed;if(!o.sees&&(direction===0||this.escapeSweep>0)){this.escapeSweep+=dt;direction=this.movement.toward(this.target.x+Math.sin(this.escapeSweep*1.5)*1.5);speed=this.movement.investigateSpeed*(this.swarmAttackBoost?this.movement.swarmAttackSpeedMultiplier:1);}distance=speed*dt;}
      else if(this.state!=='attack'&&this.waveActive){if(wave.move>0){direction=this.waveDirection;distance=this.currentMoveSpeed*wave.move;}}
      else if(this.state!=='attack'&&!this.watching&&this.inspectionRemaining<=0){
        if(this.returning||this.waitingToSleep){const delta=this.sleepPosition.x-this.x;if(Math.abs(delta)>.025){this.returning=true;this.waitingToSleep=false;this.sleepRemaining=0;direction=Math.sign(delta);distance=Math.min(Math.abs(delta),this.currentMoveSpeed*dt);}else{this.returning=false;if(!this.waitingToSleep){this.waitingToSleep=true;this.sleepRemaining=this.sleepDelay;}else this.sleepRemaining=Math.max(0,this.sleepRemaining-dt);if(this.sleepRemaining<=1e-9)this.setState('sleeping');}}
        else{const step=this.patrol?this.patrol.evaluate(this.x,this.stun>0?0:dt,this.facing,this.currentMoveSpeed):null;if(step){direction=step.direction;distance=step.distance;this.facing=step.facing;}else{direction=this.facing;if(this.x<=this.left)direction=1;else if(this.x>=this.right)direction=-1;distance=this.currentMoveSpeed*dt;turn=true;}}
      }
      let blocked=false;if(this.stun<=0&&direction)blocked=!this.movement.move(direction,distance,turn);
      if(this.swarmAttackBoost&&this.stun<=0&&!this.watchingHighFlyer)this.movement.tryJump(this.target,blocked);this.movement.simulateVertical(dt);this.attack.tryAttack(this.state==='attack'&&this.stun<=0);
    }
  }
  root.RampagerAI=RampagerAI;if(typeof module!=='undefined')module.exports=RampagerAI;
})(globalThis);
