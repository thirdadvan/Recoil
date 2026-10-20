'use strict';
const assert=require('node:assert/strict'),{Game}=require('./game.js');
function run(name,fn){fn();console.log('PASS '+name);}
function setup(n=2){const g=new Game();g.enemies=g.enemies.slice(0,n);g.pos={x:12,y:.825};g.grounded=true;g.soundWaves.remaining=100;for(const e of g.enemies){e.x=12;e.y=.8;e.identified=true;e.target={...g.pos};e.lastSight=g.time;e.setState('attack');}return g;}
run('two monsters in the same frame each deal a hit and stack one-second slow',()=>{const g=setup();assert.notEqual(g.enemies[0].attack,g.enemies[1].attack);g.tick({},.01);assert.equal(g.health,1);assert.equal(g.damageSpeedMultiplier,.0625);g.tick({},.01);assert.equal(g.health,1);});
run('three monsters can kill on their first independent attacks',()=>{const g=setup(3);g.tick({},.01);assert.equal(g.health,0);assert(g.dead);for(const e of g.enemies)assert(e.attack.cooldownRemaining>1.49);});
run('cooldowns are per monster and remain staggered across state changes',()=>{const g=setup(),[a,b]=g.enemies;g.health=g.maxHealth=10;assert(a.attack.tryAttack(true));g.time=.2;assert(b.attack.tryAttack(true));assert.equal(g.health,8);g.time=1.5;assert(a.attack.tryAttack(true));assert(!b.attack.tryAttack(true));b.setState('irritated');b.setState('attack');g.time=1.7;assert(b.attack.tryAttack(true));assert.equal(g.health,6);});
run('out of range, stun or non-attack prevents an attack without consuming cooldown',()=>{const g=setup(1),e=g.enemies[0];g.pos.x=14;assert(!e.attack.tryAttack(true));assert.equal(e.attack.cooldownRemaining,0);g.pos.x=12;e.stun=1;e.tick(.01);assert.equal(g.health,3);e.stun=0;e.setState('irritated');e.tick(.01);assert.equal(g.health,3);e.setState('attack');e.tick(.01);assert.equal(g.health,2);});
run('retry clears every monster cooldown',()=>{const g=setup();g.tick({},.01);g.reset();for(const e of g.enemies)assert.equal(e.attack.cooldownRemaining,0);});
function flight(){const g=new Game();g.enemies=g.enemies.slice(0,1);const e=g.enemies[0];g.pos={x:e.x-.9,y:1.4};g.flying=true;assert(e.canSee());return {g,e};}
run('Roaming flight now irritates immediately and uses anger before attack',()=>{const {g,e}=flight();e.tick(.01);assert.equal(e.state,'irritated');g.time+=.01;e.tick(.01);assert.equal(e.state,'irritated');});
run('visible shooting now attacks immediately',()=>{const {g,e}=flight();g.shotSequence=1;g.shotAt=g.time;e.tick(.01);assert.equal(e.state,'attack');});
run('Swarm flight now uses irritation and anger',()=>{const {e}=flight();e.joinSwarm();e.tick(.01);assert.equal(e.state,'irritated');});
