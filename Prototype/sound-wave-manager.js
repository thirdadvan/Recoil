(function(root){
  'use strict';
  class SoundWaveManager {
    constructor(game){
      this.game=game;this.buildingBroadcastsEnabled=true;this.initialDelay=12;this.interval=15;this.rounds=3;this.maxRounds=4;this.warningDuration=3;this.moveDelay=.1;this.holdDuration=1.5;this.commandDuration=1;this.reactionGrace=0;this.recoveryDuration=1.25;this.noiseSpeed=10;
      this.source={x:-35,y:2.5};this.broadcastSpeed=25;this.reset();
    }
    reset(){this.triggeredSource=null;this.phase='idle';this.round=0;this.totalWaves=this.rounds;this.nextCount=this.rounds;this.remaining=this.initialDelay;this.waves=[];this.serial=0;this.cueIndex=0;this.cueDelay=this.cueHold=this.cueMove=0;this.cueDirection=0;}
    get redAlert(){return this.phase!=='idle';}
    get direction(){return this.cueDirection;}
    get active(){return this.cueDelay<=0&&this.cueMove>0;}
    get commandAge(){return this.commandDuration-this.cueMove;}
    get waveInterval(){return this.moveDelay+this.commandDuration+this.holdDuration;}
    contains(p){const a=this.area||{x:-2,y:-5,w:41,h:20};return !!(this.triggeredSource&&this.redAlert)||this.buildingBroadcastsEnabled&&p.x>=a.x&&p.x<=a.x+a.w&&p.y>=a.y&&p.y<=a.y+a.h;}
    start(){
      if(!this.buildingBroadcastsEnabled||this.phase!=='idle')return;
      this.beginBroadcast();
    }
    startAt(source){if(this.phase!=='idle')return false;this.triggeredSource={...source};this.beginBroadcast();return true;}
    beginBroadcast(){
      this.totalWaves=this.nextCount;this.nextCount=this.totalWaves>=Math.max(this.rounds,this.maxRounds)?this.rounds:this.totalWaves+1;
      this.round=0;this.cueIndex=0;this.cueDirection=0;this.cueDelay=this.cueMove=0;this.cueHold=this.warningDuration;this.phase='warning';this.remaining=this.warningDuration;
      this.wave(this.triggeredSource||this.source,100,this.warningDuration,'warning');
      for(const e of this.game.enemies)e.receiveWarning(this.warningDuration);
      this.game.say('红色预警！灯光转红，怪物醒来并抬高视线。3 秒后发出第一波方向指令。');
    }
    emitNoise(p,radius){this.wave(p,radius,radius/Math.max(.01,this.noiseSpeed),'noise');}
    emitAid(p,radius){this.wave(p,radius,.8,'aid');}
    wave(p,radius,duration,kind,direction=0){const w={x:p.x,y:p.y,radius,duration,kind,direction,age:0,shrinking:(kind==='direction'||kind==='broadcast')&&direction<0,delivered:new Set()};this.waves.push(w);return w;}
    emitBroadcast(){
      const direction=Math.random()<.5?-1:1;
      const w=this.wave(this.triggeredSource||this.source,100,this.waveInterval,'broadcast',direction);w.id=++this.serial;w.index=this.round+1;w.total=this.totalWaves;
      this.cueIndex=w.index;this.cueDirection=direction;this.cueDelay=this.moveDelay;this.cueMove=this.cueHold=0;
      for(const e of this.game.enemies)if(e.receiveWave(w.id,w.index,w.total,direction))this.wave(e,3,this.commandDuration+this.moveDelay,'direction',direction);
    }
    tick(dt){
      if(this.phase==='warning')this.cueHold=Math.max(0,this.cueHold-dt);
      else if(this.phase==='commands'){
        const d=Math.min(dt,this.cueDelay);this.cueDelay-=d;let rest=dt-d;
        if(this.cueDelay<=0&&this.cueMove===0&&this.cueHold===0){this.cueMove=this.commandDuration;}
        const m=Math.min(rest,this.cueMove);this.cueMove-=m;rest-=m;
        if(m>0&&this.cueMove<=0)this.cueHold=this.holdDuration;
        this.cueHold=Math.max(0,this.cueHold-rest);
      }
      if(this.buildingBroadcastsEnabled||this.redAlert)this.remaining-=dt;
      if((this.buildingBroadcastsEnabled||this.redAlert)&&this.remaining<=1e-6){
        if(this.phase==='idle')this.start();
        else if(this.phase==='warning'){this.phase='commands';this.remaining+=this.waveInterval;this.emitBroadcast();}
        else if(this.phase==='commands'){
          if(this.round+1<this.totalWaves){this.round++;this.remaining+=this.waveInterval;this.emitBroadcast();}
          else{for(const e of this.game.enemies)e.endSwarm(this.recoveryDuration);this.phase='recovery';this.remaining=this.recoveryDuration;this.cueDelay=this.cueHold=this.cueMove=0;}
        }else if(this.phase==='recovery'){this.phase='idle';this.remaining=this.interval;this.triggeredSource=null;}
      }
      for(const w of this.waves){w.age+=dt;if(w.kind==='noise')for(const e of this.game.enemies)if(!w.delivered.has(e)&&Math.hypot(e.x-w.x,e.y-w.y)<=Math.min(w.radius,w.age*this.noiseSpeed)){e.hearNoise(w,w.radius);w.delivered.add(e);}}
      this.waves=this.waves.filter(w=>w.age<w.duration);
    }
  }
  root.SoundWaveManager=SoundWaveManager;if(typeof module!=='undefined')module.exports=SoundWaveManager;
})(globalThis);
