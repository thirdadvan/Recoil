'use strict';
const assert=require('node:assert/strict');
const {Game}=require('./game.js');
function run(name,fn){fn();console.log('PASS '+name);}
function isolated(){const g=new Game();g.enemies=[];g.pos={x:8,y:.825};g.grounded=true;return g;}
function step(g,n,input={}){for(let i=0;i<n;i++)g.tick(input,.01);}
run('three shots, cooldown, empty shots produce no recoil or events',()=>{
  const g=isolated();assert.equal(g.ammo,3);assert(g.shoot());assert(!g.shoot());
  g.time=.3;assert(g.shoot());g.time=.6;assert(g.shoot());assert.equal(g.ammo,0);
  const seq=g.shotSequence,at=g.shotAt,pool={...g.pool};g.time=1;assert(!g.shoot());assert.equal(g.shotSequence,seq);assert.equal(g.shotAt,at);assert.deepEqual(g.pool,pool);
});
run('ground refills exactly one per second and caps at three',()=>{
  const g=isolated();g.ammo=0;step(g,99);assert.equal(g.ammo,0);step(g,1);assert.equal(g.ammo,1);
  step(g,100);assert.equal(g.ammo,2);step(g,500);assert.equal(g.ammo,3);
});
run('no aerial refill; landing starts a fresh one-second timer',()=>{
  const g=isolated();g.ammo=0;g.refillElapsed=.9;g.pos.y=8;g.grounded=false;g.velocity.y=0;
  let count=0;while(!g.grounded&&count++<300)g.tick({},.01);
  assert(g.grounded);assert.equal(g.ammo,0);assert.equal(g.refillElapsed,0);
  step(g,99);assert.equal(g.ammo,0);step(g,1);assert.equal(g.ammo,1);
});
run('falling downward shot auto-hovers with no direction input',()=>{
  const g=isolated();g.grounded=false;g.pos.y=3;g.velocity.y=-4;
  g.tick({fire:true,aim:{x:0,y:-1}},.01);assert(g.flying);assert.equal(g.velocity.y,0);assert.equal(g.pos.y,3);assert.equal(g.ammo,2);
  step(g,100);assert(!g.flying);assert(g.pos.y<3);assert.equal(g.ammo,2);
});
run('ascending shot, horizontal shot, and empty shot do not auto-hover',()=>{
  for(const config of [{vy:2,aim:{x:0,y:-1},ammo:3},{vy:-2,aim:{x:1,y:0},ammo:3},{vy:-2,aim:{x:0,y:-1},ammo:0}]){
    const g=isolated();g.grounded=false;g.pos.y=3;g.velocity.y=config.vy;g.ammo=config.ammo;g.tick({fire:true,aim:config.aim},.01);assert(!g.flying);
  }
});
run('W-held firing still launches from ground and uses a shot',()=>{
  const g=isolated();g.tick({fire:true,y:1,aim:{x:0,y:-1}},.01);assert(g.flying);assert(g.pos.y>.825);assert.equal(g.ammo,2);
});
run('short falls never count; long falls exceed one full body height',()=>{
  let g=isolated();g.grounded=false;g.pos.y=2;let seen=false;for(let i=0;i<150;i++){g.tick({},.01);seen=seen||g.highFall;}assert(!seen);
  g=isolated();g.grounded=false;g.pos.y=5;
  while(g.fallDistance<=1.65){g.tick({},.01);if(g.fallDistance<=1.65)assert(!g.highFall);}
  assert(g.highFall);assert(g.fallSequence>0);
  while(!g.grounded)g.tick({},.01);assert(g.highFall);g.tick({},.01);assert(!g.highFall);
});
run('visible high fall irritates once; a new fall while irritated attacks',()=>{
  const g=new Game(),e=g.enemies[1];g.pos={x:24,y:1.8};e.patrol=null;
  assert(e.canSee());g.highFall=true;g.fallSequence=1;e.tick(.01);assert.equal(e.state,'irritated');
  for(let i=0;i<20;i++)e.tick(.01);assert.equal(e.state,'irritated');
  g.fallSequence=2;e.tick(.01);assert.equal(e.state,'attack');
});
run('unseen high falls are ignored; landing and reset clear transient data',()=>{
  const g=new Game(),e=g.enemies[1];g.pos={x:27,y:1};e.patrol=null;g.highFall=true;g.fallSequence=1;
  assert(!e.canSee());e.tick(.01);assert.equal(e.state,'roaming');
  g.reset(true);assert.equal(g.ammo,3);assert.equal(g.fallSequence,0);assert(!g.highFall);
});
run('visible high fall exits Swarm for irritation',()=>{
  const g=new Game(),e=g.enemies[1];g.pos={x:24,y:1.8};e.receiveWarning(3);g.highFall=true;g.fallSequence=1;e.tick(.01);assert.equal(e.state,'irritated');
});
