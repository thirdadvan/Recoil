'use strict';
const assert=require('node:assert/strict');
const {Game}=require('./game.js');
function test(name,fn){fn();console.log('PASS '+name);}
function near(a,b){assert(Math.abs(a-b)<1e-6,`${a} != ${b}`);}
function setup(state='sleeping'){
  const g=new Game(),e=g.enemies[0];g.enemies=[e];g.blocks=[];g.clear=()=>true;
  Object.defineProperty(g,'illumination',{get:()=>1});e.patrol=null;e.initialState=state;e.reset();
  g.pos={x:e.x-1,y:e.y};g.horizontalIntent=0;g.flying=false;g.activeItem=null;g.shotAt=-100;g.forbiddenUntil=-100;g.highFall=false;
  return {g,e};
}
function steps(g,e,n,dt=.01){for(let i=0;i<n;i++){g.time+=dt;e.tick(dt);}}
test('vision alone never wakes; sound takes one second before visual confirmation attacks',()=>{
  const {g,e}=setup();steps(g,e,50);assert.equal(e.state,'sleeping');assert(!e.canSee());
  e.hearNoise(g.pos,3);assert(e.waking);steps(g,e,99);assert.equal(e.state,'sleeping');steps(g,e,1);assert.equal(e.state,'attack');near(e.currentMoveSpeed,e.movement.chaseSpeed*1.5);
});
test('sound check holds 1.5 seconds, then waits ten seconds before sleeping',()=>{
  const {g,e}=setup();g.pos={x:-100,y:50};e.hearNoise({x:e.x-1,y:e.y},3);steps(g,e,100);const x=e.x;
  steps(g,e,149);assert.equal(e.x,x);assert(e.inspectionRemaining>0);steps(g,e,2);assert(e.waitingToSleep);
  steps(g,e,997);assert.equal(e.state,'roaming');steps(g,e,4);assert.equal(e.state,'sleeping');
});
test('optional return travels to sleep point before starting sleep timer',()=>{
  const {g,e}=setup('roaming');g.pos={x:-100,y:50};e.x=e.sleepPosition.x+2;e.hearNoise({x:e.x-1,y:e.y},3);
  steps(g,e,151);assert(e.returning);assert(!e.waitingToSleep);steps(g,e,230);assert(e.waitingToSleep);assert(e.sleepRemaining>9);assert(Math.abs(e.x-e.sleepPosition.x)<.025);
});
test('return disabled resumes patrol after sound inspection',()=>{
  const {g,e}=setup('roaming');e.returnToSleepAfterSound=false;g.pos={x:-100,y:50};e.hearNoise({x:e.x-1,y:e.y},3);
  const x=e.x;steps(g,e,149);near(e.x,x);steps(g,e,5);assert(e.x<x);assert(!e.returning&&!e.waitingToSleep);
});
test('near visual tracking attacks at .3 seconds, not before',()=>{
  const {g,e}=setup('roaming');steps(g,e,29);assert(e.watchingPlayer);assert.equal(e.state,'roaming');steps(g,e,1);assert.equal(e.state,'attack');
});
test('far visual tracking attacks after three seconds',()=>{
  const {g,e}=setup('roaming');g.pos.x=e.x-Math.sqrt(e.sightRange**2-.45**2)+.0001;
  assert(e.currentAttackDelay>2.999);steps(g,e,299);assert.equal(e.state,'roaming');steps(g,e,2);assert.equal(e.state,'attack');
});
test('gaze follows crossing target; occlusion resets tracking',()=>{
  const {g,e}=setup('roaming');steps(g,e,1);g.pos.x=e.x+1;steps(g,e,1);assert.equal(e.facing,1);assert(e.watchingPlayer);
  g.clear=()=>false;steps(g,e,1);near(e.detectionProgress,0);assert(!e.watchingPlayer);
});
test('wave gaze is +15 above horizontal for both directions',()=>{
  const {g,e}=setup('roaming');e.receiveWarning(3);
  for(const direction of [-1,1]){e.facing=direction;near(e.sightPitch,15);g.pos={x:e.x+direction*5,y:e.y+.45+Math.tan(15*Math.PI/180)*5};assert(e.canSee());g.pos.y=e.y+.45-5;assert(!e.canSee());}
});
test('wave hold movement has legacy .5 second tolerance',()=>{
  const {g,e}=setup('roaming');e.receiveWarning(3);steps(g,e,20);assert.equal(e.state,'roaming');g.horizontalIntent=1;steps(g,e,49);assert.equal(e.state,'roaming');steps(g,e,1);assert.equal(e.state,'attack');
});
test('wave movement requires matching direction with .5 second tolerance',()=>{
  const {g,e}=setup('roaming');g.pos.x=e.x-3;e.receiveWarning(0);e.receiveWave(1,1,3,-1);steps(g,e,10);g.horizontalIntent=-1;steps(g,e,20);assert.equal(e.state,'roaming');g.horizontalIntent=0;steps(g,e,49);assert.equal(e.state,'roaming');steps(g,e,1);assert.equal(e.state,'attack');
});
test('wave visible shooting, interaction, flight and high fall trigger immediate attack',()=>{
  for(const kind of ['shot','interaction','flight','fall']){const {g,e}=setup('roaming');e.receiveWarning(3);if(kind==='shot'){g.shotAt=g.time;g.shotSequence++;}if(kind==='interaction'){g.forbiddenUntil=g.time+1;g.interactionSequence++;}if(kind==='flight')g.flying=true;if(kind==='fall'){g.highFall=true;g.fallSequence++;}steps(g,e,1);assert.equal(e.state,'attack',kind);}
});
test('out of sight violations do not attack',()=>{
  const {g,e}=setup('roaming');e.receiveWarning(3);g.clear=()=>false;g.flying=true;g.horizontalIntent=1;steps(g,e,50);assert.equal(e.state,'roaming');
});
test('wave bonus applies once and persists through attack after event ends',()=>{
  const {g,e}=setup('roaming');e.receiveWarning(3);g.flying=true;steps(g,e,1);near(e.currentMoveSpeed,e.movement.chaseSpeed*2.1);
  e.receiveWave(1,1,3,1);e.receiveWarning(3);near(e.currentMoveSpeed,e.movement.chaseSpeed*2.1);
  e.endSwarm(.01);steps(g,e,2);assert(!e.waveActive);assert(e.swarmAttackBoost&&e.redEyes);near(e.currentMoveSpeed,e.movement.chaseSpeed*2.1);
  g.clear=()=>false;steps(g,e,2001);assert.equal(e.state,'roaming');assert(!e.swarmAttackBoost);near(e.currentMoveSpeed,e.movement.patrolSpeed);
});
test('direct water hit attacks; reset clears tracking and retained wave bonus',()=>{
  const {g,e}=setup();e.directHit(g.pos);assert.equal(e.state,'attack');e.receiveWarning(3);e.reset();assert.equal(e.state,'sleeping');assert(!e.waveActive&&!e.redEyes);near(e.detectionProgress,0);
});
test('legacy chase setting controls attack; patrol setting remains independent',()=>{
  const {g,e}=setup('roaming');e.movement.patrolSpeed=.3;e.movement.chaseSpeed=2.6;near(e.currentMoveSpeed,.3);
  e.receiveWarning(3);near(e.currentMoveSpeed,.3);e.directHit(g.pos);near(e.currentMoveSpeed,5.46);
  e.reset();e.directHit(g.pos);near(e.currentMoveSpeed,3.9);e.movement.chaseSpeed=4;near(e.currentMoveSpeed,6);
});
test('legacy attack sight range expands by 1.5 without changing roaming sight',()=>{
  const {g,e}=setup('roaming');const range=e.sightRange;e.directHit(g.pos);near(e.sightRange,range*1.5);
  e.setState('roaming');near(e.sightRange,range);
});
test('attack publishes aid; awake receiver retains old report behavior and rejects duplicates',()=>{
  const {g,e}=setup('roaming'),Other=require('./rampager-ai.js');const other=new Other(g,e.x+2,e.x,e.x+4);other.initialState='roaming';other.reset();g.enemies.push(other);
  e.directHit(g.pos);steps(g,e,1);assert.equal(other.state,'attack');
  const r={attack:true,source:e,target:g,position:{...g.pos},sightTime:g.time,id:900000};assert(other.receive(r));assert(!other.receive(r));assert(!other.receive({...r,id:900001,sightTime:g.time-4}));
});
test('aid wakes sleeping receiver but does not attack an unseen player',()=>{
  const {g,e}=setup('roaming'),Other=require('./rampager-ai.js');const other=new Other(g,e.x+2,e.x,e.x+4);other.initialState='sleeping';other.reset();g.enemies.push(other);g.clear=()=>false;
  assert(other.receive({attack:true,source:e,target:g,position:{...g.pos},sightTime:g.time,id:910000}));assert(other.waking);steps(g,other,100);assert.equal(other.state,'roaming');assert(other.inspectionRemaining>0);
});
test('lost target searches around last position using legacy investigate speed',()=>{
  const {g,e}=setup('roaming');e.directHit(g.pos);e.stun=0;g.clear=()=>false;e.target={x:e.x,y:e.y};steps(g,e,20);const x=e.x;
  steps(g,e,1);assert(e.escapeSweep>0);near(Math.abs(e.x-x),e.movement.investigateSpeed*.01);
});
test('attack loss retains six second search pause before recovery',()=>{
  const {g,e}=setup('roaming');e.directHit(g.pos);g.clear=()=>false;steps(g,e,2001);assert.equal(e.state,'roaming');assert(e.inspectionRemaining>5.9);
  const x=e.x;steps(g,e,590);near(e.x,x);steps(g,e,20);assert(e.returning||e.waitingToSleep);
});

test('passing through an unseen monster does not stop or turn its patrol',()=>{
  const {g,e}=setup('roaming');e.idealRange=.01;const facing=e.facing;
  for(const offset of [-1,-.5,.5,1]){const x=e.x;g.pos.x=x+offset;steps(g,e,1);near(e.x-x,facing*e.movement.patrolSpeed*.01);assert.equal(e.facing,facing);assert.equal(e.state,'roaming');}
});

test('wave reverse movement attacks at .5 seconds, not before',()=>{
  const {g,e}=setup('roaming');g.pos.x=e.x-3;e.receiveWave(1,1,3,-1);steps(g,e,10);g.horizontalIntent=1;steps(g,e,49);assert.equal(e.state,'roaming');steps(g,e,1);assert.equal(e.state,'attack');
});
test('wave hold-to-move clears previous action violation',()=>{
  const {g,e}=setup('roaming');g.pos.x=e.x-3;e.receiveWave(1,1,3,-1);g.horizontalIntent=-1;steps(g,e,10);assert(e.reverse>.09);steps(g,e,1);near(e.reverse,0);near(e.stopped,0);assert.equal(e.state,'roaming');
});
test('wave move-to-hold clears previous action violation',()=>{
  const {g,e}=setup('roaming');g.pos.x=e.x-3;e.receiveWave(1,1,3,-1);steps(g,e,10);g.horizontalIntent=-1;steps(g,e,60);g.horizontalIntent=1;steps(g,e,39);assert(e.reverse>.38);steps(g,e,1);g.horizontalIntent=0;steps(g,e,1);near(e.reverse,0);near(e.stopped,0);assert.equal(e.state,'roaming');
});
test('wave compliance, sight loss and blocked movement clear violation counters',()=>{
  const {g,e}=setup('roaming');e.receiveWarning(3);g.horizontalIntent=1;steps(g,e,40);assert(e.reverse>.39);g.horizontalIntent=0;steps(g,e,1);near(e.reverse,0);
  g.horizontalIntent=1;steps(g,e,40);g.clear=()=>false;steps(g,e,1);near(e.reverse,0);g.clear=()=>true;steps(g,e,40);g.movementBlocked=true;steps(g,e,1);near(e.reverse,0);assert.equal(e.state,'roaming');
});

test('wake uses the latest sound without restarting the one second timer',()=>{
 const {g,e}=setup();g.pos={x:e.x+2,y:e.y};const x=e.x;e.hearNoise({x:e.x-1,y:e.y},3);steps(g,e,60);e.hearNoise({x:e.x+1,y:e.y},3);near(e.wakeRemaining,.4);steps(g,e,39);assert.equal(e.state,'sleeping');near(e.x,x);steps(g,e,1);assert.equal(e.facing,1);assert.equal(e.state,'attack');
});
test('waking turns to latest sound, does not identify a player behind it',()=>{
 const {g,e}=setup();g.pos={x:e.x-2,y:e.y};e.hearNoise({x:e.x-1,y:e.y},3);steps(g,e,50);e.hearNoise({x:e.x+1,y:e.y},3);steps(g,e,50);assert.equal(e.facing,1);assert.equal(e.state,'roaming');assert(!e.waking);
});
test('direct water hit interrupts waking immediately and reset clears it',()=>{
 const {g,e}=setup();e.hearNoise(g.pos,3);steps(g,e,20);e.directHit(g.pos);assert.equal(e.state,'attack');assert(!e.waking);e.reset();assert(!e.waking);near(e.wakeRemaining,0);
});

test('waking looks at elevated latest sound location before visual check',()=>{
 const {g,e}=setup();g.pos={x:e.x+2,y:e.y+3};e.hearNoise(g.pos,5);steps(g,e,100);assert.equal(e.state,'attack');
});


test('a sleeping monster returns directly to sleep after broadcast recovery',()=>{
 const {g,e}=setup();g.pos={x:-100,y:50};e.returnToSleepAfterSound=false;e.receiveWarning(3);assert.equal(e.state,'roaming');e.receiveWarning(3);assert(e.sleepingBeforeWave);e.receiveWave(1,1,3,1);e.endSwarm(.02);steps(g,e,1);assert.equal(e.state,'roaming');steps(g,e,1);assert.equal(e.state,'sleeping');assert(!e.waveActive&&!e.redEyes&&!e.waking);assert(!e.returning&&!e.waitingToSleep);
});
test('a monster sleeping before broadcast returns to sleep even after a wave attack',()=>{
 const {g,e}=setup();e.receiveWarning(3);e.directHit(g.pos);assert.equal(e.state,'attack');assert(e.swarmAttackBoost);e.endSwarm(.01);steps(g,e,1);assert.equal(e.state,'sleeping');assert(!e.swarmAttackBoost&&!e.redEyes);assert(!e.sleepingBeforeWave);
});
test('wave sleep memory uses entry state, is cleared on reset and captured again next cycle',()=>{
 const {g,e}=setup();g.pos={x:-100,y:50};e.receiveWarning(3);e.reset();assert(!e.sleepingBeforeWave);e.setState('roaming');e.returnToSleepAfterSound=false;e.receiveWave(1,1,3,-1);e.endSwarm(.01);steps(g,e,1);assert.equal(e.state,'roaming');
 e.setState('sleeping');e.receiveWave(2,1,3,1);e.endSwarm(.01);steps(g,e,1);assert.equal(e.state,'sleeping');
});
