'use strict';
const assert=require('node:assert/strict');
const Patrol=require('./rampager-patrol.js');
const {Game}=require('./game.js');
function run(name,fn){fn();console.log('PASS '+name);}
function point(order,x,action='stay',duration=1){return {order,x,action,duration,lookInterval:.5};}
run('sort by order, preserving author order for ties',()=>{
  const route=new Patrol([point(2,5),point(0,1),point(2,3)]);
  assert.deepEqual(route.points.map(p=>p.x),[1,5,3]);
});
run('travel clamps to destination and time begins only after arrival',()=>{
  const route=new Patrol([point(0,1,'lookRight',1)],false);
  let step=route.evaluate(0,3,-1,.9);assert.equal(step.distance,1);assert.equal(route.elapsed,0);assert.equal(route.arrived,false);
  step=route.evaluate(1,.4,-1,.9);assert.equal(step.direction,0);assert.equal(step.facing,1);assert.equal(route.elapsed,.4);assert.equal(route.complete,false);
  route.evaluate(1,.59,1,.9);assert.equal(route.complete,false);route.evaluate(1,.02,1,.9);assert.equal(route.complete,true);
});
run('stay preserves arrival facing; explicit left/right actions override it',()=>{
  for(const [action,input,expected] of [['stay',1,1],['stay',-1,-1],['lookLeft',1,-1],['lookRight',-1,1]]){
    const route=new Patrol([point(0,0,action,2)]);assert.equal(route.evaluate(0,.1,input,.9).facing,expected);
  }
});
run('look around switches actual facing every configured interval',()=>{
  const route=new Patrol([point(0,0,'lookAround',3)]);
  assert.equal(route.evaluate(0,.1,1,.9).facing,-1);
  assert.equal(route.evaluate(0,.5,1,.9).facing,1);
  assert.equal(route.evaluate(0,.5,1,.9).facing,-1);
});
run('loop wraps; one-shot holds final facing after completion',()=>{
  const route=new Patrol([point(0,0,'lookRight',.1),point(1,0,'lookLeft',.1)]);
  route.evaluate(0,.1,-1,.9);assert.equal(route.index,1);route.evaluate(0,.1,1,.9);assert.equal(route.index,0);
  const once=new Patrol([point(0,0,'lookRight',.1)],false);once.evaluate(0,.1,-1,.9);
  assert.equal(once.evaluate(0,100,-1,.9).facing,1);assert(once.complete);
});
run('coincident zero-duration looping points terminate without hanging',()=>{
  const route=new Patrol([point(0,0,'stay',0),point(1,0,'stay',0)]);
  assert.equal(route.evaluate(0,1,-1,.9).direction,0);
});
run('interruption keeps target, resets action and returns to fixed destination',()=>{
  const route=new Patrol([point(0,0,'lookAround',2)]);route.evaluate(0,.7,-1,.9);route.suspend();
  const step=route.evaluate(3,.1,1,.9);assert.equal(step.direction,-1);assert.equal(route.elapsed,0);assert.equal(route.current.x,0);
  route.evaluate(0,.1,1,.9);assert.equal(route.elapsed,.1);
});
run('game collision prevents arrival and consuming hold time',()=>{
  const g=new Game(),e=g.enemies[1];g.pos={x:2,y:11};g.blocks.push({x:24,y:0,w:.2,h:2});e.patrol=new Patrol([point(0,23,'lookLeft',2)]);
  for(let i=0;i<300;i++){g.time+=.01;e.tick(.01);}
  assert(e.x>24.2);assert.equal(e.patrol.elapsed,0);assert.equal(e.patrol.arrived,false);
});
run('Swarm and irritation immediately interrupt patrol; roam resumes same point',()=>{
  const g=new Game(),e=g.enemies[1];g.pos={x:2,y:11};e.patrol=new Patrol([point(0,e.x,'lookAround',2)]);
  e.tick(.4);assert.equal(e.patrol.elapsed,.4);
  e.receiveWarning(3);assert.equal(e.state,'swarm');assert.equal(e.patrol.elapsed,0);
  e.tick(.5);assert.equal(e.patrol.elapsed,0);
  e.setState('irritated');const x=e.x;e.tick(.5);assert.equal(e.x,x);
  e.setState('roaming');e.tick(.2);assert.equal(e.patrol.elapsed,.2);
});
run('reset restarts the route; no route retains legacy patrol',()=>{
  const g=new Game(),e=g.enemies[1];g.pos={x:2,y:11};e.patrol=new Patrol([point(0,e.x,'stay',2)]);
  e.tick(.5);e.reset();assert.equal(e.patrol.elapsed,0);assert.equal(e.patrol.index,0);
  e.patrol=null;const x=e.x;e.tick(.2);assert(e.x<x);
});
