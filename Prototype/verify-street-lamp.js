'use strict';
const assert=require('node:assert/strict'),{Game}=require('./game.js'),StreetLamp=require('./street-lamp.js');
function run(name,fn){fn();console.log('PASS '+name);}
run('downward cone bounds, above and outside excluded',()=>{const l=new StreetLamp();assert(l.contains({x:22,y:.8}));assert(!l.contains({x:22,y:5}));assert(!l.contains({x:24,y:4}));assert(!l.contains({x:22,y:-.1}));});
run('player center inside cone gives exactly full exposure, including cone edge',()=>{const g=new Game();g.pos={x:23.63,y:.8};assert(g.streetLamps[0].contains(g.pos));assert.equal(g.illumination,1);const e=g.enemies[1];assert.equal(e.sightRange,e.idealRange);});
run('head-only instant interaction extinguishes lighting and invalidates same-frame cache',()=>{const g=new Game(),l=g.streetLamps[0];g.pos={x:22,y:.825};assert.equal(g.illumination,1);assert(!l.interact(g));g.pos={x:22,y:4};assert.equal(g.illumination,1);assert(l.interact(g));assert(!l.isOn);assert(g.illumination<1);assert(!g.nearbyStreetLamp);assert(!l.interact(g));});
run('E switches head instantly without hold progress and is a forbidden interaction',()=>{const g=new Game(),l=g.streetLamps[0];g.enemies=[];g.pos={x:22,y:4};g.grounded=false;g.tick({interact:true},.01);assert(!l.isOn);assert.equal(g.interactionSequence,1);assert.equal(g.activeItem,null);});
run('reusable lamps have independent state and retry restores lamps',()=>{const g=new Game();g.streetLamps.push(new StreetLamp({x:30}));g.pos={x:22,y:4};g.streetLamps[0].interact(g);assert(g.streetLamps[1].isOn);g.reset();assert(g.streetLamps[0].isOn);});
