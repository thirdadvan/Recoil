'use strict';
const assert=require('node:assert/strict'),{Game}=require('./game.js');
function game(){const g=new Game(null);g.enemies=[];g.blocks=[{x:-20,y:-1,w:40,h:1}];g.pos={x:0,y:.825};g.grounded=true;return g;}
function step(g,n,input={}){for(let i=0;i<n;i++)g.tick(input,.01);}
function run(name,fn){fn();console.log('PASS '+name);}
run('ladder ascends, holds without gravity, descends, and stays at the top',()=>{
  const g=game();g.ladders=[{x:-.5,y:0,w:1,h:4,climbSpeed:2.4}];
  step(g,50,{y:1});assert(g.climbing);assert(Math.abs(g.pos.y-2.025)<1e-8);
  const held=g.pos.y;step(g,50);assert.equal(g.pos.y,held);
  step(g,20,{y:-1});assert(g.pos.y<held);
  step(g,250,{y:1});assert(g.climbing);assert(Math.abs(g.pos.y-4.825)<1e-8);
});
run('ladder exits by horizontal movement or jump; shooting starts flight',()=>{
  for(const input of [{x:1},{jump:true},{fire:true,aim:{x:1,y:0}}]){
    const g=game();g.ladders=[{x:-.5,y:0,w:1,h:5,climbSpeed:2.4}];step(g,50,{y:1});g.tick(input,.01);assert(!g.climbing);
    if(input.fire)assert(g.flying);if(input.jump)assert(g.velocity.y>0);
  }
});
run('soft roof passes upward, lands downward, and permits walking and crouching',()=>{
  const g=game();g.softRoofs=[{x:-3,y:2,w:6,h:.2}];g.pos.y=1;g.move(0,3);assert.equal(g.pos.y,4);
  g.move(0,-3);assert(Math.abs(g.pos.y-3.025)<1e-8);assert(g.grounded);
  step(g,20,{x:1});assert(g.pos.x>.4);assert(Math.abs(g.pos.y-3.025)<1e-8);
  step(g,20,{sneak:true});assert(g.grounded);assert(Math.abs(g.pos.y-2.625)<1e-8);
});
run('down plus jump drops through the current roof and lands on the next roof',()=>{
  const g=game();g.softRoofs=[{x:-3,y:3,w:6,h:.2},{x:-3,y:1,w:6,h:.2}];g.pos.y=4.025;g.grounded=true;
  g.tick({y:-1,jump:true},.01);assert(!g.grounded);assert(g.velocity.y<0);assert(!g.flying);
  step(g,150);assert(g.grounded);assert(Math.abs(g.pos.y-2.025)<1e-8);
});
run('shots cross the soft roof in both directions',()=>{
  for(const [y,aim] of [[1,{x:0,y:1}],[4,{x:0,y:-1}]]){
    const g=game();g.blocks=[];g.softRoofs=[{x:-3,y:2,w:6,h:.2}];g.pos.y=y;g.aim=aim;g.shoot();
    const spray=g.sprays[0];assert(Math.abs(spray.end.y-spray.start.y)>5.9);
  }
});
