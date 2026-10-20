/* Rendering and controls are separate from the deterministic gameplay model. */
'use strict';
const game = new Recoil.Game(), canvas = document.querySelector('#game'), ctx = canvas.getContext('2d');
const overlay = document.querySelector('#overlay'), content = document.querySelector('#content');
const keys = new Set(), pressed = new Set();
let mouseLookActive=false;
let started = false, paused = false, firing = false, mouse = { x: 180, y: 600 }, previous = 0, screen = '';
let scale=game.scene?720/(2*game.scene.camera.viewSize):48;
const sx = x => (x - game.camera) * scale + 640, sy = y => game.scene?360-(y-game.cameraY)*scale:590-y*scale;
const rect = (x, y, w, h, color) => { ctx.fillStyle = color; ctx.fillRect(sx(x), sy(y + h), w * scale, h * scale); };
function menu(kind) {
  if (screen === kind) return; screen = kind; overlay.hidden = false;
  const views = {
    start: ['RECOIL', '<p class="intro">一名侦探，一把塑料水枪，一座失序的小镇。</p><p>按住 <kbd>W</kbd> 并朝下射击，就能直接起飞（Space 也可以）。<br>水枪最多三发，落地后每秒补一发；下落时朝下射击会自动悬停。<br>刚下车没有水枪：先进入左侧加油站超市，在后方柜台拿枪；入口损坏后，飞起打开中央天窗，从天窗回到屋顶。随后搜索背包获取员工卡，再进入工厂。</p><div class="controls"><span><kbd>A</kbd> <kbd>D</kbd> 左右移动</span><span><kbd>W</kbd> <kbd>S</kbd> 飞行升降</span><span>鼠标瞄准 / 按住左键射击</span><span><kbd>C</kbd> 安静蹲行 / <kbd>Shift</kbd> 静音行走</span><span><kbd>Space</kbd> 跳跃 / 起飞</span><span><kbd>E</kbd> 按住搜索 / 调查 / 离开</span></div><p class="hint">休眠怪物会被声音唤醒；苏醒需要 1 秒，之后朝最近声源检查；看见玩家立即攻击。<br>漫游视线朝下 25°；听到声音会转向声源检查 1.5 秒。看见玩家后持续跟踪，按距离在 0.3～3 秒内攻击。<br>搜索背包需按住 E 1.5 秒；音波期间，被看见的违规行为会立即触发攻击。<br>广播：红色预警后 3 秒发出第一波；扩张圈向右、收缩圈向左，每波随机。收到后 0.1 秒开始移动 1 秒，再静止等待下一波；每轮 3 或 4 波。B 测试。</p>', '进入小镇'],
    dead: ['被发现了', '<p class="intro">它比我想的更近。</p><p>漫游怪物持续看见你会进入攻击。声波期间，保持静止或跟随方向指令。<br>水枪会引敌，也能短暂打停敌人，为撤离争取时间。</p><p class="hint">R 从最近检查点重试 · Shift + R 从头开始</p>', '重新尝试'],
    won: ['一个地址，一段记忆。', `<p class="intro">“我知道这个地方。但愿我不知道。”</p><p>线索已找到 · ${Math.floor(game.elapsed)} 秒 · ${game.attempts} 次尝试</p><p class="hint">第一版短关卡完成。可以再试一条更安静的路线。<br>试试广播的转向提示，以及怪物休眠、声音调查、视觉跟踪和攻击的行为。</p>`, '再玩一次'],
    paused: ['暂停', '<p>回到游戏后，点击画面即可继续用鼠标瞄准。</p>', '继续']
  };
  const [heading, body, action] = views[kind];
  content.innerHTML = `<h1>${heading}</h1>${body}<button id="begin">${action}</button>`;
  document.querySelector('#begin').onclick = () => {
    if (kind === 'dead') { game.attempts++; game.reset(false); }
    if (kind === 'won') game.reset(true);
    started = true; paused = false; screen = ''; overlay.hidden = true;
    keys.clear(); pressed.clear(); firing = false; previous = performance.now(); canvas.focus();
  };
}
document.querySelector('#sleep-test').onclick=()=>{if(!started)return;if(game.sleepTest)game.reset(false);else game.beginSleepTest();paused=false;screen='';overlay.hidden=true;firing=false;keys.clear();pressed.clear();canvas.focus();};
document.querySelector('#wake-test').onclick=()=>{game.wakeNearestForTest();canvas.focus();};
document.addEventListener('keydown', e => {
  if (['Space','ArrowUp','ArrowDown','ArrowLeft','ArrowRight'].includes(e.code)) e.preventDefault();
  if (!keys.has(e.code)) pressed.add(e.code); keys.add(e.code);
  if(e.code==='KeyE'&&!e.repeat&&paused&&game.activeReadable){game.closeReadable();pressed.delete('KeyE');return;}
  if (e.code === 'KeyR' && !e.repeat && started) {
    game.attempts++; game.reset(e.shiftKey); paused = false; screen = ''; overlay.hidden = true; firing = false;
  }
  if(e.code==='Escape'&&!e.repeat&&game.activeReadable){game.closeReadable();pressed.delete('Escape');return;}
  if (e.code === 'Escape' && !e.repeat && started && !game.dead && !game.won) {
    paused = !paused; if (!paused) { screen = ''; overlay.hidden = true; } else menu('paused');
  }
});
document.addEventListener('keyup', e => keys.delete(e.code));
canvas.addEventListener('pointermove', e => { mouseLookActive=true; const r = canvas.getBoundingClientRect(); mouse = { x: (e.clientX-r.left)*1280/r.width, y: (e.clientY-r.top)*720/r.height }; });
canvas.addEventListener('pointerleave',()=>{mouseLookActive=false;});
canvas.addEventListener('pointerdown', e => { if (e.button === 0) { firing = true; canvas.focus(); canvas.setPointerCapture(e.pointerId); } });
document.addEventListener('pointerup', () => { firing = false; });
window.addEventListener('blur', () => { keys.clear(); pressed.clear(); firing = false; if (started && !game.dead && !game.won) { paused = true; menu('paused'); } });
document.addEventListener('visibilitychange', () => { if (document.hidden) { keys.clear(); firing = false; paused = true; } });
function person(p, player = false) {
  const crouch = player && game.crouched, color = player ? '#acbab3' : p.stun > 0 ? '#76cddd' : p.state==='sleeping'?'#665751':p.alert > .5 ? '#d47466' : '#a16b66';
  const h = crouch ? .85 : 1.65;
  ctx.save(); ctx.translate(sx(p.x), sy(p.y));ctx.scale(scale/48,scale/48);
  ctx.fillStyle = '#080f13'; ctx.fillRect(-13, h*24-8, 10, 9); ctx.fillRect(3,h*24-8,10,9);
  ctx.fillStyle = color; ctx.fillRect(-14,-h*24+17,28,h*48-25);
  ctx.fillStyle = player ? '#d4c8ad' : '#ad8b7b'; ctx.fillRect(-10,-h*24,20,19);
  if(player) { ctx.fillStyle='#567477';ctx.fillRect(-16,-h*24-3,32,6);ctx.fillRect(-10,-h*24-12,20,11);ctx.strokeStyle='#728b8b';ctx.beginPath();ctx.moveTo(0,-h*24+20);ctx.lineTo(0,h*24-10);ctx.stroke(); }
  else { ctx.fillStyle=p.redEyes?'#ff3434':p.state==='sleeping'&&!p.waking?'#25201f':'#f0d098';ctx.fillRect(p.facing>0?6:-9,-h*24+8,4,p.state==='sleeping'&&!p.waking?1:3); }
  ctx.restore();
}
const lightCanvas=document.createElement('canvas');lightCanvas.width=1280;lightCanvas.height=720;
const lightCtx=lightCanvas.getContext('2d');
function renderLighting(){
  const alarm=game.soundWaves.redAlert;
  const ambient=Math.round(255*game.lighting.outdoorAmbient*(game.lighting.unlitAmbientScale??1)),area=game.lighting.factoryArea;
  lightCtx.globalCompositeOperation='source-over';lightCtx.fillStyle=`rgb(${ambient},${alarm?Math.round(ambient*.18):ambient},${alarm?Math.round(ambient*.12):ambient})`;lightCtx.fillRect(0,0,1280,720);
  const flash=Math.round(255*game.lighting.swarmAmbient);
  lightCtx.fillStyle=`rgb(${flash},${Math.round(flash*.45)},${Math.round(flash*.3)})`;if(area.enabled)lightCtx.fillRect(sx(area.minX),sy(area.maxY),(area.maxX-area.minX)*scale,(area.maxY-area.minY)*scale);
  lightCtx.globalCompositeOperation='lighten';
  const lights=[{x:game.pos.x,y:game.pos.y,radius:game.lighting.playerLightRadius,intensity:game.lighting.playerLightIntensity},
    ...game.lighting.selectedLights];
  for(const light of lights){
    const x=sx(light.x),y=sy(light.y),r=light.radius*scale;
    const gradient=lightCtx.createRadialGradient(x,y,0,x,y,r);
    for(const t of [0,.25,.5,.75,1]){const value=Math.round(255*light.intensity*(1-t*t*(3-2*t)));gradient.addColorStop(t,`rgb(${value},${alarm?Math.round(value*.18):value},${alarm?Math.round(value*.12):value})`);}
    lightCtx.fillStyle=gradient;lightCtx.beginPath();lightCtx.arc(x,y,r,0,Math.PI*2);lightCtx.fill();
  }
  for(const lamp of game.streetLamps)if(lamp.isOn){
    lightCtx.fillStyle=alarm?'rgb(255,46,31)':'rgb(255,255,255)';lightCtx.beginPath();lightCtx.moveTo(sx(lamp.x),sy(lamp.y));const dir=lamp.direction,end={x:lamp.x+dir.x*lamp.coneLength,y:lamp.y+dir.y*lamp.coneLength};lightCtx.lineTo(sx(end.x-dir.y*lamp.coneHalfWidth),sy(end.y+dir.x*lamp.coneHalfWidth));lightCtx.lineTo(sx(end.x+dir.y*lamp.coneHalfWidth),sy(end.y-dir.x*lamp.coneHalfWidth));lightCtx.closePath();lightCtx.fill();
  }
  ctx.save();ctx.globalCompositeOperation='multiply';ctx.drawImage(lightCanvas,0,0);ctx.restore();
}
function renderSightShadows(){
  if(!game.scene)return;
  const origin=game.pos,reach=Math.max(100,4*game.scene.camera.viewSize*(1+1280/720));
  ctx.save();ctx.fillStyle='#000';
  for(const block of game.blocks){
    if(block.blocksSight===false)continue;
    if(origin.x>=block.x&&origin.x<=block.x+block.w&&origin.y>=block.y&&origin.y<=block.y+block.h)continue;
    const corners=[{x:block.x,y:block.y},{x:block.x+block.w,y:block.y},{x:block.x+block.w,y:block.y+block.h},{x:block.x,y:block.y+block.h}];
    for(let i=0;i<4;i++){
      const a=corners[i],b=corners[(i+1)%4],extend=p=>{const dx=p.x-origin.x,dy=p.y-origin.y,d=Math.hypot(dx,dy)||1;return {x:p.x+dx/d*reach,y:p.y+dy/d*reach};},farA=extend(a),farB=extend(b);
      ctx.beginPath();ctx.moveTo(sx(a.x),sy(a.y));for(const p of [b,farB,farA])ctx.lineTo(sx(p.x),sy(p.y));ctx.closePath();ctx.fill();
    }
  }
  ctx.restore();
}
function renderSoundWaves(){
  const sw=game.soundWaves;
  for(const w of sw.waves){
    const t=Math.min(1,w.age/w.duration);
    if(w.kind==='broadcast'||w.kind==='warning'){
      // A distant source, with an immediately visible advancing / receding front.
      const fromTower=!!sw.triggeredSource;
      const cx=fromTower?sx(w.x):-720,cy=fromTower?sy(w.y):360;
      const reach=fromTower?Math.hypot(cx-640,cy-360)+Math.hypot(640,360):0;
      const radius=fromTower?reach*(w.shrinking?1-t:t):760+(w.shrinking?1-t:t)*1040,color=w.kind==='warning'?'255,52,52':w.direction>0?'255,199,125':'119,215,255';
      ctx.save();ctx.shadowColor=`rgb(${color})`;ctx.shadowBlur=12;
      for(let n=0;n<3;n++){ctx.strokeStyle=`rgba(${color},${[.95,.55,.3][n]})`;ctx.lineWidth=n===0?4:2;ctx.beginPath();ctx.arc(cx,cy,Math.max(1,radius-n*34),0,Math.PI*2);ctx.stroke();}
      ctx.restore();continue;
    }
    const radius=Math.max(.03,w.radius*(w.shrinking?1-t:t))*scale;
    ctx.save();ctx.strokeStyle=w.kind==='noise'?`rgba(138,229,255,${.3*(1-t)})`:w.kind==='aid'?`rgba(212,156,255,${.65*(1-t)})`:w.direction>0?`rgba(255,199,125,${.8*(1-t)})`:`rgba(119,215,255,${.8*(1-t)})`;
    ctx.lineWidth=w.kind==='noise'?1.2:2.2;if(w.kind==='aid')ctx.setLineDash([7,5]);ctx.beginPath();ctx.arc(sx(w.x),sy(w.y),radius,0,Math.PI*2);ctx.stroke();ctx.restore();
    if(w.kind==='aid'){ctx.font='12px "PingFang SC", "Microsoft YaHei", sans-serif';ctx.fillStyle='#d49cff';ctx.fillText('攻击报告 / 最后目击位置',sx(w.x)-55,sy(w.y)-48);}
  }
  if(!started||game.dead||game.won||!sw.contains(game.pos))return;
  ctx.save();ctx.fillStyle=sw.phase==='warning'?'#400e16f2':'#0b202cf2';ctx.fillRect(280,106,720,87);ctx.textAlign='center';
  const color=sw.phase==='warning'?'#ff6464':sw.direction>0?'#ffc77d':'#77d7ff',dir=sw.direction>0?'→ 向右 / 扩张圈':'← 向左 / 收缩圈';
  ctx.fillStyle=sw.phase==='idle'||sw.phase==='recovery'?'#b4cbd3':color;ctx.font='bold 22px "PingFang SC", "Microsoft YaHei", sans-serif';
  const phase=sw.cueDelay>0?`收到指令 · ${sw.cueDelay.toFixed(1)} 秒后移动`:sw.active?`移动 ${sw.cueMove.toFixed(1)} 秒`:`静止等待下一波 · ${Math.max(0,sw.remaining).toFixed(1)} 秒`;
  const main=sw.phase==='idle'?`远处声波 · ${Math.max(0,sw.remaining).toFixed(1)} 秒后预警 · B 测试`:sw.phase==='warning'?`红色预警！保持静止 · ${Math.max(0,sw.remaining).toFixed(1)} 秒后第一波`:sw.phase==='recovery'?'本轮结束 · 红色灯光即将恢复':`第 ${sw.cueIndex}/${sw.totalWaves} 波 · ${phase} · ${dir}`;
  ctx.fillText(main,640,136);ctx.font='14px "PingFang SC", "Microsoft YaHei", sans-serif';ctx.fillStyle='#c2d2d7';
  ctx.fillText('扩张 → 右 ｜ 收缩 ← 左 ｜ 每波随机 ｜ 收到后 0.1 秒移动 1 秒，再静止等待',640,161);
  const progress=sw.phase==='warning'?sw.remaining/sw.warningDuration:sw.cueDelay>0?sw.cueDelay/sw.moveDelay:sw.active?sw.cueMove/sw.commandDuration:sw.cueHold/sw.holdDuration;
  if(sw.redAlert){ctx.fillStyle='#29424d';ctx.fillRect(302,177,676,4);ctx.fillStyle=color;ctx.fillRect(302,177,676*Math.max(0,Math.min(1,progress)),4);}
  ctx.restore();
}
const sceneImages=new Map();
function drawSceneSprite(sprite){
  const [a,b,c,d,x,y]=sprite.matrix,[w,h]=sprite.size,color=sprite.color;
  ctx.save();ctx.transform(a*scale,-b*scale,-c*scale,d*scale,sx(x),sy(y));
  if(sprite.image){let image=sceneImages.get(sprite.image);if(!image){image=new Image();image.src=sprite.image;sceneImages.set(sprite.image,image);}if(image.complete&&image.naturalWidth){ctx.globalAlpha=color.a;ctx.drawImage(image,-w/2,-h/2,w,h);}}
  else{ctx.fillStyle=`rgba(${Math.round(color.r*255)},${Math.round(color.g*255)},${Math.round(color.b*255)},${color.a})`;ctx.fillRect(-w/2,-h/2,w,h);}
  ctx.restore();
}
function renderMonsterStates(){
  document.querySelector('#sleep-test-tools').hidden=!started||paused;
  document.querySelector('#sleep-test').textContent=game.sleepTest?'退出测试并重置':'休眠测试';
  document.querySelector('#wake-test').hidden=!game.sleepTest;
  if(!started)return;
  const counts={sleeping:0,waking:0,roaming:0,attack:0};
  for(const e of game.enemies)counts[e.waking?'waking':e.state]++;
  ctx.save();ctx.fillStyle='#09141de8';ctx.fillRect(18,106,248,78);ctx.fillStyle='#e0e5db';ctx.font='14px "PingFang SC", "Microsoft YaHei", sans-serif';
  ctx.fillText(`怪物状态 · 共 ${game.enemies.length} 只`,30,127);
  ctx.fillStyle='#a5bbbf';ctx.fillText(`休眠 ${counts.sleeping}  苏醒 ${counts.waking}  漫游 ${counts.roaming}  攻击 ${counts.attack}`,30,149);
  const nearest=[...game.enemies].sort((a,b)=>Math.abs(a.x-game.pos.x)-Math.abs(b.x-game.pos.x))[0];
  if(nearest){const state=nearest.waking?`苏醒 ${nearest.wakeRemaining.toFixed(1)}s`:nearest.state==='sleeping'?'休眠 / 等待声音':nearest.state==='attack'?'攻击':nearest.waitingToSleep?`休眠倒计时 ${nearest.sleepRemaining.toFixed(1)}s`:nearest.returning?'返回休眠点':nearest.investigate>0?`声音检查 ${nearest.investigate.toFixed(1)}s`:nearest.watchingPlayer?`视觉发现 ${Math.round(nearest.detectionProgress*100)}%`:nearest.waveActive?'声波指令':'漫游';ctx.fillText('最近怪物：'+state,30,171);}
  ctx.restore();
}
function renderFuelPumps(afterLighting=false){
  for(const pump of game.items.filter(item=>item.kind==='fuelPump')){
    const broken=game.inventory.has(pump.resultId),screen=pump.screen;
    if(afterLighting===broken){
      rect(screen.x,screen.y,screen.w,screen.h,broken?'#b20904':'#153f3a');
      ctx.save();ctx.textAlign='center';ctx.textBaseline='middle';ctx.font=`${Math.max(7,Math.min(12,screen.w*scale/8))}px sans-serif`;ctx.fillStyle=broken?'#ffb99b':'#99d9b8';
      ctx.fillText(broken?'Out of order':'IN SERVICE',sx(screen.x+screen.w/2),sy(screen.y+screen.h/2));ctx.restore();
    }
    if(!afterLighting){
      const held=game.time<pump.pickupUntil,p=held?{x:game.pos.x+game.aim.x*.55,y:game.pos.y+game.aim.y*.55+.12}:pump.nozzle;
      ctx.save();ctx.translate(sx(p.x),sy(p.y));ctx.rotate(held?-Math.atan2(game.aim.y,game.aim.x):25*Math.PI/180);ctx.fillStyle='#949e87';ctx.fillRect(-.125*scale,-.18*scale,.25*scale,.36*scale);ctx.restore();
    }
  }
}
function renderAmmoRing(){
  if(!started||paused||game.dead||game.won||game.activeReadable||!game.hasWaterGun||!Number.isFinite(game.ammoUIAt))return;
  const t=Math.max(0,Math.min(1,(game.time-game.ammoUIAt-2)/.5)),alpha=1-t*t*(3-2*t);
  if(alpha<=0)return;
  const count=Math.max(1,game.ammoCapacity),step=Math.PI*2/count,gap=Math.min(.087,step*.2);
  ctx.save();ctx.translate(sx(game.pos.x+game.aim.x*.5),sy(game.pos.y+game.aim.y*.5)-36);ctx.globalAlpha=alpha;ctx.lineWidth=3;ctx.lineCap='butt';
  for(let i=0;i<count;i++){
    ctx.beginPath();ctx.arc(0,0,18,-Math.PI/2+i*step+gap/2,-Math.PI/2+(i+1)*step-gap/2);
    ctx.strokeStyle=i<game.ammo?'#7ae6ff':'#334d59';ctx.stroke();
  }
  ctx.restore();
}
function render() {
  scale=game.scene?720/(2*game.scene.camera.viewSize):48;
  const background=ctx.createLinearGradient(0,0,0,720);background.addColorStop(0,'#101b25');background.addColorStop(1,'#24343a');
  ctx.fillStyle=background;ctx.fillRect(0,0,1280,720);
  if(game.scene){
    for(const sprite of game.scene.sprites)drawSceneSprite(sprite);
  }else{
    for(let i=0;i<20;i++){const x=i*3-5,h=3+(i*7%6);rect(x,0,2.3,h,'#1b2a32');}
    for(const b of game.blocks){rect(b.x,b.y,b.w,b.h,'#303f45');rect(b.x,b.y+b.h-.07,b.w,.07,'#778b89');}
  }
  for(const door of game.doors){
    if(door.artwork){drawSceneSprite(door.artwork);}
    else if(door.kind==='office'){if(!door.hatchOpen(game))rect(door.x-.08,door.y-1.2,.16,2.4,'#7b725c');else rect(door.x+.18,door.y-1.2,.55,2.3,'#53594c');}
    else if(door.openStateItemId&&!door.openStateItemId.startsWith('factory_shortcut_')){rect(door.x-1.5,door.y-.09,3,.18,door.hatchOpen(game)?'#1d3034':'#708e94');if(door.hatchOpen(game))rect(door.x+1.4,door.y,.16,1.15,'#708e94');}
    else{rect(door.x-.55,door.y-1.25,1.1,2.5,door.blocked(game)?'#463c36':door.isOpen?'#457b65':'#294b45');rect(door.x-.45,door.y+1.3,.9,.12,'#87c5a2');if(door.blocked(game)){ctx.strokeStyle='#986b59';ctx.lineWidth=6;ctx.beginPath();ctx.moveTo(sx(door.x-.45),sy(door.y-.4));ctx.lineTo(sx(door.x+.45),sy(door.y+.4));ctx.stroke();}}
  }
  if(game.scene)for(const label of game.scene.texts||[]){ctx.font=`${Math.max(10,label.size*scale*4)}px sans-serif`;ctx.fillStyle='#a6beb4';ctx.textAlign='center';ctx.fillText(label.text,sx(label.x),sy(label.y));}ctx.textAlign='left';
  for(const bag of game.items){
    if(bag.kind==='fuelPump')continue;
    if(game.inventory.has(bag.resultId))continue;
    if(bag.resultId==='water_gun'){rect(bag.x-.32,bag.y-.1,.64,.2,'#50ceef');rect(bag.x-.12,bag.y-.28,.12,.22,'#2d94ad');continue;}
    rect(bag.x-.32,bag.y-.45,.64,.9,bag.completed?'#536361':'#b68b55');
    rect(bag.x-.22,bag.y-.22,.44,.32,'#725739');rect(bag.x-.18,bag.y+.45,.36,.1,'#d8b47c');
    rect(bag.x-.27,bag.y-.3,.055,.7,'#ddbb86');rect(bag.x+.21,bag.y-.3,.055,.7,'#ddbb86');
  }
  for(const e of game.enemies){
    person(e);ctx.font='12px "PingFang SC", "Microsoft YaHei", sans-serif';ctx.fillStyle=e.state==='attack'?'#ff8f77':'#afc7d2';ctx.textAlign='center';
    const label=e.waking?`苏醒中 ${e.wakeRemaining.toFixed(1)}s`:e.state==='sleeping'?'休眠':e.state==='attack'?(e.watchingHighFlyer?'攻击 / 抬头注视':e.swarmAttackBoost?'攻击 / 音波强化':'攻击'):e.waveActive?(e.waveMoving?(e.waveDirection<0?'← 音波向左':'音波向右 →'):'音波 / 静止'):e.watchingPlayer?`跟踪 · ${Math.round(e.detectionProgress*100)}%`:e.investigate>0?`声音检查 ${e.investigate.toFixed(1)}s`:e.returning?'返回休眠点':e.waitingToSleep?`${e.sleepRemaining.toFixed(1)}s 后休眠`:'漫游';
    ctx.fillText(label,sx(e.x),sy(e.y+1.7));ctx.textAlign='left';if(e.alert>.02){ctx.fillStyle='#443b37';ctx.fillRect(sx(e.x)-23,sy(e.y+1.35),46,4);ctx.fillStyle='#ee997d';ctx.fillRect(sx(e.x)-23,sy(e.y+1.35),46*e.alert,4);}
  }
  person(game.pos,true);
  for(const chain of game.chains){
    const links=Math.ceil(chain.length/chain.linkSpacing),spacing=chain.length/links,d=chain.direction;
    ctx.save();ctx.strokeStyle='#6e787a';ctx.lineWidth=Math.max(1,.032*scale);
    for(let i=0;i<links;i++){
      ctx.beginPath();for(let j=0;j<=12;j++){const a=j*Math.PI/6,r=(i+.5)*spacing+Math.cos(a)*spacing*.48,w=Math.sin(a)*(i%2===0?.075:.04);const x=chain.x+d.x*r+Math.cos(chain.angle)*w,y=chain.y+d.y*r+Math.sin(chain.angle)*w;if(j===0)ctx.moveTo(sx(x),sy(y));else ctx.lineTo(sx(x),sy(y));}ctx.stroke();
    }ctx.restore();
  }
  if(game.hasWaterGun){ctx.save();ctx.translate(sx(game.pos.x),sy(game.pos.y));ctx.scale(scale/48,scale/48);ctx.rotate(-Math.atan2(game.aim.y,game.aim.x));ctx.fillStyle=game.playerSettings.automaticRecoil?'#f28a32':'#50ceef';ctx.fillRect(12,-5,28,9);ctx.fillStyle=game.playerSettings.automaticRecoil?'#a94f1e':'#2d94ad';ctx.fillRect(18,4,7,9);ctx.fillStyle=game.playerSettings.automaticRecoil?'#ffc779':'#9be9ff';ctx.fillRect(34,-4,8,7);ctx.restore();}
  for(const s of game.sprays){ctx.strokeStyle=`rgba(136,231,255,${1-(game.time-s.at)/.16})`;ctx.lineWidth=3;ctx.beginPath();ctx.moveTo(sx(s.start.x),sy(s.start.y));ctx.lineTo(sx(s.end.x),sy(s.end.y));ctx.stroke();}
  for(const lamp of game.streetLamps){if(!game.scene)rect(lamp.x+.44,lamp.y-lamp.coneLength,.12,lamp.coneLength,'#525c64');rect(lamp.x-.4,lamp.y-.08,.8,.16,lamp.isOn?'#ffe4a0':'#384047');}
  renderFuelPumps();
  renderLighting();
  renderFuelPumps(true);
  for(const trigger of game.scene?.towerTriggers||[]){
    const flash=game.inventory.has(trigger.progressId)&&game.soundWaves.redAlert;
    const t=flash?Math.pow(.5+.5*Math.sin(game.time*Math.PI*2/trigger.blinkPeriod),3):0;
    const c=flash?{r:.12+.88*t,g:.01+.07*t,b:.005+.02*t}:trigger.restColor;
    const b=trigger.beacon;rect(b.x,b.y,b.w,b.h,`rgb(${Math.round(c.r*255)},${Math.round(c.g*255)},${Math.round(c.b*255)})`);
  }
  renderAmmoRing();
  for(const lamp of game.streetLamps)if(lamp.isOn){ctx.save();ctx.fillStyle='#ffe4a0';ctx.shadowColor='#ffe4a0';ctx.shadowBlur=12;ctx.fillRect(sx(lamp.x)-19,sy(lamp.y)-4,38,8);ctx.restore();}
  for(const e of game.enemies)if(e.watchingPlayer&&Math.hypot(e.x-game.pos.x,e.y-game.pos.y)<game.lighting.playerLightRadius){
    ctx.save();ctx.font='12px "PingFang SC", "Microsoft YaHei", sans-serif';ctx.textAlign='center';ctx.fillStyle='#ffd59a';ctx.fillText(`跟踪 · ${Math.round(e.detectionProgress*100)}%`,sx(e.x),sy(e.y+1.7));ctx.restore();
  }
  // Red eyes remain readable in the dark without illuminating the player's surroundings.
  for(const e of game.enemies)if(e.redEyes){
    ctx.save();ctx.fillStyle='#ff3434';ctx.shadowColor='#ff2020';ctx.shadowBlur=12;
    ctx.fillRect(sx(e.x)+(e.facing>0?6:-9),sy(e.y)-1.65*24+8,5,4);ctx.restore();
  }
  renderSoundWaves();
  renderSightShadows();
  renderMonsterStates();
  if(started && !game.dead && !game.won && !paused){
    ctx.strokeStyle='#8dd2dd99';ctx.lineWidth=1;ctx.beginPath();ctx.arc(mouse.x,mouse.y,6,0,Math.PI*2);ctx.stroke();
    ctx.font='16px "PingFang SC", "Microsoft YaHei", sans-serif';ctx.fillStyle='#dfdbc6';ctx.textAlign='center';
    if(game.nearbyReadable)ctx.fillText(game.nearbyReadable.prompt,sx(game.nearbyReadable.x),sy(game.nearbyReadable.y+1));
    if(game.nearbyStreetLamp)ctx.fillText('E · 关闭路灯（瞬间互动）',sx(game.nearbyStreetLamp.x),sy(game.nearbyStreetLamp.y+.5));
    const bag=game.nearbyItem;
    if(bag){
      ctx.fillText((bag.duration>0?'按住 E · ':'E · ')+bag.name,sx(bag.x),sy(1.9));
      if(game.activeItem){
        ctx.fillStyle='#101a20';ctx.fillRect(350,488,580,76);
        ctx.fillStyle='#43504e';ctx.fillRect(390,505,500,8);ctx.fillStyle='#e2c88d';ctx.fillRect(390,505,500*bag.progress/bag.duration,8);
        ctx.fillStyle='#dfdbc6';ctx.fillText('搜索中 · 松开 E 取消；音波期间被目击互动会直接遭到攻击',640,546);
      }
    }
    if((!game.scene||game.scene.legacyEvidenceEnabled)&&!game.hasClue && Recoil.distance(game.pos,{x:31.5,y:.8})<1.5)ctx.fillText('E · 查看纸条',sx(31.5),sy(1.9));
    if(game.nearbyDoor)ctx.fillText(game.nearbyDoor.prompt(game),sx(game.nearbyDoor.x),sy(game.nearbyDoor.y+2.25));ctx.textAlign='left';
  }
  document.querySelector('#objective').textContent=game.scene?(game.inventory.has('employee_access_card')?game.scene.objectiveWithCard:game.scene.objectiveWithoutCard):'搜索背包获取员工卡，再从门进入工厂。';
  document.querySelector('#status').textContent=(game.scene?game.scene.label:game.checkpoint?'工厂 / 已设检查点':'室外院子')+(game.inventory.has('employee_access_card')?' · 背包：员工门禁卡':'')+' · '+(game.climbing?'攀爬中':game.flying?'悬浮中':game.crouched?'安静蹲行':game.silentWalking?'静音行走':game.grounded?'地面':'下落中')+` · ${game.hasWaterGun?"水枪已装备":"未持有水枪"} · 生命 ${game.health}/${game.maxHealth} · 速度 ${(game.damageSpeedMultiplier*100).toFixed(2)}% · 受光 ${Math.round(game.illumination*100)}%`;
  document.querySelector('#bar').hidden=!game.flying;document.querySelector('#fuel').style.width=Recoil.clamp(1-(game.time-game.shotAt),0,1)*100+'%';
  const speech=document.querySelector('#speech');const message=game.bottomDialogue;speech.hidden=!started || paused || game.dead || game.won || !!game.activeReadable || !message;speech.textContent=message;
  const reader=document.querySelector('#reader'),picture=document.querySelector('#reader-image'),note=document.querySelector('#reader-text');
  reader.hidden=!game.activeReadable;
  if(game.activeReadable){const page=game.activeReadable;picture.hidden=!page.image;note.hidden=!!page.image;if(page.image&&reader.displayedPage!==page){picture.src=page.image;reader.displayedPage=page;}picture.alt=page.name;note.textContent=page.text||page.name;}
}
function frame(now){
  scale=game.scene?720/(2*game.scene.camera.viewSize):48;
  const dt=previous?Math.min((now-previous)/1000,.033):.016;previous=now;
  if(started && !paused){
    const has=k=>keys.has(k);
    game.tick({x:Number(has('KeyD')||has('ArrowRight'))-Number(has('KeyA')||has('ArrowLeft')),
      y:has('KeyS')||has('ArrowDown')?-1:Number(has('KeyW')||has('ArrowUp')||has('Space')),
      sneak:has('KeyC')||has('ControlLeft'),silentWalk:has('ShiftLeft')||has('ShiftRight'),fire:firing,jump:pressed.has('Space'),launch:pressed.has('KeyW')||pressed.has('ArrowUp'),interact:pressed.has('KeyE'),interactHeld:has('KeyE'),broadcast:pressed.has('KeyB'),
      testWaterGun:pressed.has('KeyT'),mouseLookActive,aim:{x:(mouse.x-640)/scale+game.camera-game.pos.x,y:(game.scene?game.cameraY+(360-mouse.y)/scale:(590-mouse.y)/scale)-game.pos.y}},dt);
    if(game.dead)menu('dead');else if(game.won)menu('won');
  }
  pressed.clear();render();requestAnimationFrame(frame);
}
menu('start');requestAnimationFrame(frame);
