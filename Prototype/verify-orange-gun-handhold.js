'use strict';
const assert=require('node:assert/strict'),{Game}=require('./game.js');
function run(name,fn){fn();console.log('PASS '+name);}
function setup(ground=false){const g=new Game(null);g.enemies=[];g.chains=[];g.ladders=[];g.softRoofs=[];g.handholds=[];g.soundWaves.remaining=1000;g.blocks=ground?[{x:-100,y:-1,w:200,h:1}]:[];g.pos={x:0,y:ground?.825:5};g.grounded=ground;return g;}
run('downward shot launches automatically from ground and air without direction input',()=>{for(const grounded of [true,false]){const g=setup(grounded),y=g.pos.y;g.aim={x:0,y:-1};g.tick({fire:true},.01);assert(g.pos.y>y);assert(g.flying);}});
run('horizontal and upward shots push opposite the aim',()=>{for(const aim of [{x:1,y:0},{x:0,y:1}]){const g=setup(),before={...g.pos};g.aim=aim;g.tick({fire:true},.01);assert((g.pos.x-before.x)*aim.x+(g.pos.y-before.y)*aim.y<0);}});
run('pool ends at decay time, then hover lasts an additional half second',()=>{const g=setup();g.aim={x:0,y:-1};g.shoot();for(let n=0;n<76;n++)g.tick({},.01);const y=g.pos.y;for(let n=0;n<43;n++)g.tick({},.01);assert(g.flying);assert(Math.abs(g.pos.y-y)<1e-8);for(let n=0;n<8;n++)g.tick({},.01);assert(!g.flying);assert(g.pos.y<y);});
function withHold(){const g=setup();const h={x:-.1,y:5.5,w:.3,h:.3,position:{x:0,y:5},releaseCooldown:.5};g.handholds=[h];g.tick({},.01);assert.equal(g.holdingHandhold,h);return g;}
run('touching automatically holds and suspends gravity',()=>{const g=withHold();for(let n=0;n<100;n++)g.tick({x:1,y:-1},.01);assert.deepEqual(g.pos,{x:0,y:5});});
run('shooting releases, applies recoil, and prevents immediate regrab',()=>{const g=withHold();g.aim={x:0,y:-1};g.tick({fire:true},.01);assert(!g.holdingHandhold);assert(g.flying);assert(g.pos.y>5);g.tick({},.01);assert(!g.holdingHandhold);});
run('jumping releases and moves upward without immediate regrab',()=>{const g=withHold();g.tick({jump:true},.01);assert(!g.holdingHandhold);assert(g.pos.y>5);assert(!g.flying);});

run('orange gun enforces one second between shots',()=>{const g=setup();g.shoot();assert.equal(g.shotSequence,1);g.time=.99;g.shoot();assert.equal(g.shotSequence,1);g.time=1;g.shoot();assert.equal(g.shotSequence,2);});
