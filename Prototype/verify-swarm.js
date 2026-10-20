'use strict';
const assert=require('node:assert/strict');
const {Game}=require('./game.js');
function setup(){const g=new Game(),e=g.enemies[1];g.lighting.fixedLights=[{x:25,y:1,radius:100,intensity:1}];g.pos={x:23,y:.825};return {g,e};}
function step(g,e,intent,dt=.01){g.time+=dt;g.horizontalIntent=intent;g.pos={x:e.x+e.facing*2,y:.825};e.tick(dt);}
function steps(g,e,intent,count){for(let i=0;i<count;i++)step(g,e,intent);}
function run(name,test){test();console.log('PASS '+name);}
run('red warning immediately overrides Irritated; first command waits three seconds',()=>{
  const {g,e}=setup();e.setState('irritated');g.soundWaves.start();
  assert(g.soundWaves.redAlert);assert.equal(g.soundWaves.phase,'warning');assert.equal(g.soundWaves.waves[0].kind,'warning');assert.equal(e.state,'swarm');assert.equal(e.waveIndex,0);
  g.pos={x:2,y:11};const x=e.x;
  for(let i=0;i<299;i++){g.time+=.01;g.soundWaves.tick(.01);e.tick(.01);}
  assert.equal(e.x,x);assert.equal(e.waveIndex,0);
  g.soundWaves.tick(.01);assert.equal(e.waveIndex,1);assert.equal(g.soundWaves.phase,'commands');assert.equal(e.waveDelay,.1);
});
run('direction command delays 0.1s, moves one second, then waits 1.5s',()=>{
  const {g,e}=setup();e.receiveWave(1,1,3,-1);const x=e.x;
  steps(g,e,0,10);assert(Math.abs(e.x-x)<1e-8);
  steps(g,e,-1,100);assert(Math.abs(e.x-(x-.9))<1e-8);assert.equal(e.state,'swarm');
  const end=e.x;steps(g,e,0,149);assert.equal(e.x,end);assert.equal(e.state,'swarm');assert(e.waveHold>0);
});
run('each wave independently randomizes direction; expanding is right, shrinking is left',()=>{
  const {g}=setup();const original=Math.random;let calls=0;const samples=[.8,.1,.8];
  try{Math.random=()=>samples[calls++];g.soundWaves.start();for(let n=0;n<300;n++)g.soundWaves.tick(.01);
    for(let i=0;i<3;i++){
      const w=g.soundWaves.waves.find(w=>w.kind==='broadcast'&&w.index===i+1);
      assert(w);assert.equal(w.direction,[1,-1,1][i]);assert.equal(w.shrinking,w.direction<0);
      if(i<2)for(let n=0;n<260;n++)g.soundWaves.tick(.01);
    }
    assert.equal(calls,3);
  }finally{Math.random=original;}
});
run('following complete random commands stays safe and the warning ends cleanly',()=>{
  const {g,e}=setup();g.soundWaves.start();
  for(let i=0;i<1300;i++){
    g.time+=.01;g.soundWaves.tick(.01);g.pos={x:e.x+e.facing*2,y:.825};
    g.horizontalIntent=e.waveMove>0&&e.waveDelay<.009999?e.waveDirection:0;e.tick(.01);
    assert.notEqual(e.state,'attack');assert.notEqual(e.state,'irritated');
  }
  assert.equal(e.waveIndex,3);assert.equal(e.state,'roaming');assert.equal(g.soundWaves.phase,'idle');assert(!g.soundWaves.redAlert);
  g.soundWaves.start();assert.equal(g.soundWaves.totalWaves,4);
});
for(const intent of [0,1])run((intent?'reverse':'standing still')+' during MOVE attacks at 0.5s',()=>{
  const {g,e}=setup();e.receiveWave(1,1,3,-1);steps(g,e,0,10);steps(g,e,intent,49);assert.equal(e.state,'swarm');step(g,e,intent);assert.equal(e.state,'attack');
});
run('movement while waiting or during red warning attacks at 0.5s',()=>{
  const {g,e}=setup();e.receiveWarning(3);steps(g,e,1,49);assert.equal(e.state,'swarm');step(g,e,1);assert.equal(e.state,'attack');
});
run('MOVE and waiting timers are reset independently',()=>{
  const {g,e}=setup();e.receiveWave(1,1,3,-1);steps(g,e,0,10);steps(g,e,-1,55);steps(g,e,1,45);assert.equal(e.state,'swarm');
  steps(g,e,1,25);assert.equal(e.state,'swarm');assert(e.reverse<.26);
  e.receiveWave(2,2,3,1);assert.equal(e.reverse,0);assert.equal(e.stopped,0);
});
run('Attack has priority over the red warning and direction commands',()=>{
  const {g,e}=setup();e.directHit(g.pos);g.soundWaves.start();assert.equal(e.state,'attack');assert(!e.receiveWave(1,1,3,1));
});
run('terrain blockage and losing sight remain exempt',()=>{
  const {g,e}=setup();e.receiveWarning(3);g.movementBlocked=true;steps(g,e,1,100);assert.equal(e.state,'swarm');assert.equal(e.reverse,0);
  g.movementBlocked=false;for(let i=0;i<100;i++){g.time+=.01;g.pos={x:2,y:11};g.horizontalIntent=1;e.tick(.01);}assert.equal(e.state,'swarm');
});
