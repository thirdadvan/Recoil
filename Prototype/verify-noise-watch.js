'use strict';
const assert=require('node:assert/strict');
const {Game}=require('./game.js');
function run(name,fn){fn();console.log('PASS '+name);}
function near(a,b){assert(Math.abs(a-b)<1e-7,`${a} != ${b}`);}
function setup(){const g=new Game(),e=g.enemies[1];g.enemies=[e];g.pos={x:24,y:.825};g.grounded=true;g.movementVelocity={x:0,y:0};g.soundWaves.remaining=1000;return {g,e};}
function steps(g,e,n,moving=false){g.movementVelocity={x:moving?1:0,y:0};for(let i=0;i<n;i++){g.time+=.01;e.tick(.01);}}
run('controller has independent perception and movement instances',()=>{
  const {g,e}=setup();assert(e.perception&&e.movement);e.facing=1;assert(!e.perception.canSee());e.movement.lookAt(g.pos.x);assert(e.perception.canSee());
});
run('roaming sound stops patrol, turns toward player, ignores sounds out of range',()=>{
  const {g,e}=setup();e.hearNoise({x:0,y:.8},3);assert(!e.watchingPlayer);e.hearNoise(g.pos,3.2);assert(e.watchingPlayer);assert.equal(e.state,'roaming');assert.equal(e.facing,-1);
  const x=e.x;steps(g,e,50);assert.equal(e.x,x);near(e.noiseAnger,.25);
});
run('two seconds stationary angers without immediately attacking',()=>{
  const {g,e}=setup();e.hearNoise(g.pos,3.2);steps(g,e,199);assert.equal(e.state,'roaming');steps(g,e,1);assert.equal(e.state,'irritated');assert(!e.watchingPlayer);
});
run('motion continuously reduces anger; resumed stillness adds from remaining anger',()=>{
  const {g,e}=setup();e.hearNoise(g.pos,3.2);steps(g,e,100);near(e.noiseAnger,.5);steps(g,e,50,true);near(e.noiseAnger,.25);steps(g,e,50);near(e.noiseAnger,.5);
});
run('repeated player noises extend watch but never reset accumulated anger',()=>{
  const {g,e}=setup();e.hearNoise(g.pos,3.2);steps(g,e,100);e.hearNoise(g.pos,3.2);near(e.noiseAnger,.5);steps(g,e,100);assert.equal(e.state,'irritated');
});
run('complete crossing adds 50% once, not for center-line jitter',()=>{
  const {g,e}=setup();e.hearNoise(g.pos,3.2);steps(g,e,1);e.noiseAnger=.3;g.movementVelocity={x:2,y:0};
  for(const x of [24.6,24.9,25.1,25.4]){g.pos.x=x;g.time+=.01;e.tick(.01);assert(e.noiseAnger<.31);}
  g.pos.x=25.7;g.time+=.01;e.tick(.01);near(e.noiseAnger,.775);g.pos.x=26;g.time+=.01;e.tick(.01);near(e.noiseAnger,.77);
});
run('crossing can fill anger and enter irritation; passing above the body does not count',()=>{
  let {g,e}=setup();e.hearNoise(g.pos,3.2);steps(g,e,1);e.noiseAnger=.6;g.movementVelocity={x:2,y:0};
  for(const x of [24.6,25.2,25.7]){g.pos.x=x;g.time+=.01;e.tick(.01);}assert.equal(e.state,'irritated');
  ({g,e}=setup());e.hearNoise(g.pos,3.2);steps(g,e,1);e.noiseAnger=.3;g.pos.y=3;g.movementVelocity={x:2,y:0};
  for(const x of [24.6,25.2,25.7]){g.pos.x=x;g.time+=.01;e.tick(.01);}assert(e.noiseAnger<.3);
});
run('watch expires back to patrol, sight loss does not increase anger',()=>{
  const {g,e}=setup();e.hearNoise(g.pos,3.2);g.blocks.push({x:24.4,y:0,w:.2,h:3});steps(g,e,100);near(e.noiseAnger,0);steps(g,e,251);assert(!e.watchingPlayer);assert.equal(e.state,'roaming');
});
run('anger reaching full on the watch deadline takes priority over expiry',()=>{
  const {g,e}=setup();e.hearNoise(g.pos,3.2);steps(g,e,150,true);steps(g,e,200);assert.equal(e.state,'irritated');
});
run('Swarm and Attack immediately cancel watch; player noises cannot override them',()=>{
  const {g,e}=setup();e.hearNoise(g.pos,3.2);steps(g,e,50);e.receiveWarning(3);assert(!e.watchingPlayer);near(e.noiseAnger,0);e.hearNoise(g.pos,3.2);assert(!e.watchingPlayer);
  e.directHit(g.pos);e.hearNoise(g.pos,3.2);assert.equal(e.state,'attack');assert(!e.watchingPlayer);
});
run('actual player noise ripple reaches and starts a roaming watch',()=>{
  const {g,e}=setup();g.soundWaves.emitNoise(g.pos,3.2);
  for(let i=0;i<20;i++){g.time+=.01;g.soundWaves.tick(.01);e.tick(.01);}assert(e.watchingPlayer);
});
run('passing slows horizontal movement exactly 30%, exits restore speed, no stacking',()=>{
  const {g,e}=setup();e.setState('irritated');g.pos.x=e.x;g.velocity.x=3.6;let x=g.pos.x;g.tick({x:1},.01);near(g.pos.x-x,3.6*.7*.01);near(g.passageSpeedMultiplier,.7);
  g.pos.x=20;g.velocity.x=3.6;x=g.pos.x;g.tick({x:1},.01);near(g.pos.x-x,3.6*.01);near(g.passageSpeedMultiplier,1);
  g.enemies.push(e);g.pos.x=e.x;near(g.passageMultiplier(g.pos),.7);
});
run('passing slowdown preserves gravity and does not apply above monsters',()=>{
  const {g,e}=setup();e.setState('irritated');g.pos={x:e.x,y:1.3};g.grounded=false;g.velocity={x:3.6,y:-2};const y=g.pos.y;g.tick({x:1},.01);near(y-g.pos.y,2.14*.01);near(g.passageSpeedMultiplier,.7);
  g.pos.y=4;near(g.passageMultiplier(g.pos),1);
});
