'use strict';
const assert=require('node:assert/strict');
const {Game}=require('./game.js');
function run(name,fn){fn();console.log('PASS '+name);}
function dark(){const g=new Game();g.pos={x:30,y:.825};g.lighting.factoryArea.enabled=true;g.lighting.outdoorAmbient=.28;return g;}
function near(a,b){assert(Math.abs(a-b)<1e-8,`${a} != ${b}`);}
run('complete darkness is 0.4 and excludes all player self-light',()=>{
  const g=dark();near(g.illumination,.4);g.lighting.playerLightRadius=100;g.lighting.playerLightIntensity=1;g.time+=.01;near(g.illumination,.4);
});
run('fully lit is 1; environmental brightness normalizes against reference',()=>{
  const g=dark();g.lighting.fixedLights=[{x:30,y:.825,radius:10,intensity:.6}];g.time+=.01;near(g.illumination,1);
  g.lighting.fixedLights=[];g.pos={x:8,y:1};g.time+=.01;near(g.illumination,.4+.6*.28/.45);
});
run('three body samples average partial illumination',()=>{
  const g=dark();g.lighting.fixedLights=[{x:30,y:.825,radius:1,intensity:.35}];g.time+=.01;
  const edge=1-.65*.65*(3-2*.65);near(g.illumination,.4+.6*.35/.45*(1+2*edge)/3);
});
run('overlapping lamps use max, matching renderer rather than adding strengths',()=>{
  const g=dark();const lamp={x:30,y:.825,radius:1,intensity:.35};g.lighting.fixedLights=[lamp];g.time+=.01;const first=g.illumination;
  g.lighting.fixedLights=[lamp,{...lamp}];g.time+=.01;near(g.illumination,first);
});
run('disabled lamps are excluded and visible fixed-light limit is respected',()=>{
  const g=dark();g.lighting.fixedLights=[{x:30,y:.825,radius:10,intensity:1,enabled:false}];g.time+=.01;near(g.illumination,.4);
  g.lighting.fixedLights=Array.from({length:7},()=>({x:0,y:0,radius:1,intensity:0}));g.lighting.fixedLights.push({x:30,y:.825,radius:10,intensity:1});g.time+=.01;near(g.illumination,.4);
});
run('red beacon brightens exposure and darkness restores its floor',()=>{
  const g=dark();g.soundWaves.start();near(g.illumination,.4);g.time+=.15;near(g.illumination,1);assert(g.lighting.swarmAmbient>.35);
  g.time=.7;near(g.illumination,.4);g.soundWaves.reset();g.time+=.01;near(g.illumination,.4);near(g.lighting.swarmAmbient,0);
});
run('effective range is ideal range times illumination, retaining Attack multiplier',()=>{
  const g=dark(),e=g.enemies[1];near(e.sightRange,(32/3)*.4);e.setState('attack');near(e.sightRange,(32/3)*.4*1.5);
});
run('lighting changes visibility at the same distance; FOV and obstruction still apply',()=>{
  const g=dark(),e=g.enemies[1];e.x=36;e.facing=-1;assert(!e.canSee());
  g.lighting.fixedLights=[{x:30,y:.825,radius:10,intensity:1}];g.time+=.01;assert(e.canSee());
  e.facing=1;assert(!e.canSee());e.facing=-1;g.blocks.push({x:31,y:0,w:.4,h:4});assert(!e.canSee());
});
run('exposure is shared by all monsters and sampled once per simulation moment',()=>{
  const g=dark();let calls=0;const original=g.lighting.environmentBrightness.bind(g.lighting);g.lighting.environmentBrightness=p=>{calls++;return original(p);};
  g.illumination;for(const e of g.enemies)e.sightRange;assert.equal(calls,3);
  g.time+=.01;g.illumination;assert.equal(calls,6);g.pos.x+=.1;g.illumination;assert.equal(calls,9);
});
run('direct water hits still attack even when outside visual range',()=>{
  const g=dark(),e=g.enemies[1];e.directHit(g.pos);assert.equal(e.state,'attack');
});
