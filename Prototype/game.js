(function (root) {
  'use strict';
  const SoundManager=typeof module!=='undefined'?require('./sound-wave-manager.js'):root.SoundWaveManager;
  const Rampager=typeof module!=='undefined'?require('./rampager-ai.js'):root.RampagerAI;
  const Patrol=typeof module!=='undefined'?require('./rampager-patrol.js'):root.RampagerPatrolRoute;
  const Items=typeof module!=='undefined'?require('./continuous-items.js'):root.ContinuousItems;
  const Door=typeof module!=='undefined'?require('./door.js'):root.RecoilDoor;
  const StreetLamp=typeof module!=='undefined'?require('./street-lamp.js'):root.StreetLamp;
  const Lighting=typeof module!=='undefined'?require('./lighting.js'):root.SceneLighting;
  const Stairway=typeof module!=='undefined'?require('./walkable-stairway.js'):root.WalkableStairway;
  const Chain=typeof module!=='undefined'?require('./swinging-chain.js'):root.SwingingChain;
  const clamp = (v, a, b) => Math.max(a, Math.min(b, v));
  const distance = (a, b) => Math.hypot(a.x - b.x, a.y - b.y);
  const overlap = (a, b) => a.x < b.x + b.w && a.x + a.w > b.x && a.y < b.y + b.h && a.y + a.h > b.y;
  function rayRect(p, d, r, range) {
    let near = 0, far = range;
    for (const [axis, size] of [['x', 'w'], ['y', 'h']]) {
      if (Math.abs(d[axis]) < 0.00001) {
        if (p[axis] < r[axis] || p[axis] > r[axis] + r[size]) return range;
      } else {
        let a = (r[axis] - p[axis]) / d[axis], b = (r[axis] + r[size] - p[axis]) / d[axis];
        if (a > b) [a, b] = [b, a];
        near = Math.max(near, a); far = Math.min(far, b);
        if (near > far) return range;
      }
    }
    return far >= 0 && near <= range ? Math.max(0, near) : range;
  }
  class Game {
    constructor(sceneConfig=root.RecoilSceneConfig||null) {
      this.sceneConfig=sceneConfig;this.sceneId=sceneConfig?sceneConfig.initialScene:null;
      this.blocks = [
        { x: -2, y: -1, w: 42, h: 1 }, { x: -2, y: 0, w: .5, h: 14 }, { x: 39, y: 0, w: .5, h: 14 },
        { x: 16, y: 6, w: 23, h: .6 }, { x: 24.5, y: 3.35, w: 6, h: .35, platform: true }
      ];
      this.time = 0;this.lighting=new Lighting(this); this.reset(true);
    }
    reset(full = false) {
      const retained=!full&&this.inventory?[...this.inventory].filter(id=>id==='water_gun'||(id.startsWith('station_')||id.startsWith('factory_shortcut_'))):[];
      this.activeReadable=null;this.readables=[];this.dialogueTriggers=[];this.ladders=[];this.softRoofs=[];this.climbingLadder=null;this.dropThroughRoof=null;this.ladderReattachAt=0;this.sleepTest=false;
      this.chains=[];this.climbingChain=null;
      this.stairways=[];this.walkingStairway=null;this.stairReattachAt=0;
      this.handholds=[];this.holdingHandhold=null;this.handholdReattachAt=0;
      if(full && this.sceneConfig)this.sceneId=this.sceneConfig.initialScene;
      this.lighting.reset();
      if (full) { this.checkpoint = false; this.elapsed = 0; this.attempts = 1; }
      this.pos = { x: this.checkpoint ? 18.6 : 2, y: 1.1 };
      this.velocity = { x: 0, y: 0 }; this.pool = { x: 0, y: 0 }; this.aim = { x: 1, y: 0 };
      this.poolAt = this.shotAt = this.upwardAt = this.noiseAt = -10;
      this.dialogue='';this.dialogueUntil=-Infinity;this.nextShot = 0; this.footstepAt = 0;this.nextFlightNoise=0;this.flightNoiseStarted=this.wasUsingRecoil=false;
      this.ammoCapacity=3;this.ammo=3;this.refillElapsed=0;this.refillStarted=false;this.ammoUIAt=-Infinity;this.fallDistance=0;this.fallSequence=0;this.fallActive=this.highFall=false;
      this.flying = this.grounded = this.crouched = this.dead = this.won = this.hasClue = false;
      this.sprays = []; this.noises = [];
      this.streetLamps=this.lighting.streetLamps.map(definition=>new StreetLamp(definition));this.doors=[new Door()];
      this.items=[new Items.Backpack()];this.inventory=new Set(retained);this.activeItem=null;this.interactionHeld=false;this.interactionSession=this.shotSequence=this.interactionSequence=0;
      this.horizontalIntent=0;this.movementBlocked=false;this.forbiddenUntil=-100;
      this.movementVelocity={x:0,y:0};this.silentWalking=false;
      this.maxHealth=3;this.health=3;this.damageSpeedMultiplier=1;this.damageSlowDuration=1;this.damageSlowExpiries=[];this.healthRecoveryInterval=1;this.healthRecoveryElapsed=0;this.lastHitAt=-100;
      this.enemies = [[12,9.8,15.4],[25,22.5,28.5],[32.8,30.3,36.3]].map(([x,left,right])=>new Rampager(this,x,left,right));
      for(const enemy of this.enemies)enemy.patrol=Patrol.example(enemy.home.x,enemy.left,enemy.right);
      if(!this.soundWaves)this.soundWaves=new SoundManager(this);else this.soundWaves.reset();
      this.playerSettings={walkSpeed:3.6,sneakSpeed:1.65,hoverSpeed:2.2,verticalHoverSpeed:1.5,gravity:14,jumpSpeed:5,shotInterval:1,shotRange:6,recoilDecayDuration:.75,flightDuration:1.25,groundRefillInterval:1};
      this.playerSettings.automaticRecoil=true;this.playerSettings.postRecoilFlightDuration=.5;
      this.noiseSettings={footstepRadius:3.2,footstepInterval:.45};
      this.scene=null;
      this.mouseLookOffset={x:0,y:0};
      if(this.sceneConfig)this.applyScene(this.sceneConfig.scenes[this.sceneId]);
      this.camera = this.scene?this.pos.x+this.scene.camera.followOffset.x:clamp(this.pos.x + 3, 12, 28);
      this.cameraY=this.scene?this.cameraTargetY():4.8;
      this.refreshProgress();this.say(this.checkpoint ? '又回到门口。这次，慢一点。' : '一把塑料水枪。我好像记得，它能让我飞起来。');
    }
    beginSleepTest(){
      this.sleepTest=true;this.soundWaves.buildingBroadcastsEnabled=false;this.soundWaves.reset();
      this.dead=this.won=false;
      for(const e of this.enemies){e.setState('sleeping');e.waking=false;e.wakeRemaining=0;}
      this.say('休眠测试：工厂广播暂停。脚步声或射击可以唤醒怪物；也可点击发声按钮。退出测试会重置本场景。');
    }
    wakeNearestForTest(){
      if(!this.sleepTest)return;
      const e=[...this.enemies].sort((a,b)=>Math.hypot(a.x-this.pos.x,a.y-this.pos.y)-Math.hypot(b.x-this.pos.x,b.y-this.pos.y))[0];
      if(e){const source={x:e.x-1,y:e.y};e.hearNoise(source,3);this.soundWaves.wave(source,3,.8,'testSound');this.say('测试声音已发送给最近怪物：苏醒1秒后朝声源检查。');}
    }
    applyScene(scene){
      if(!scene)throw new Error('Missing scene '+this.sceneId);
      this.scene=scene;this.blocks=scene.blocks.map(b=>({...b}));this.ladders=(scene.ladders||[]).map(l=>({...l}));this.softRoofs=(scene.softRoofs||[]).map(r=>({...r}));this.pos={...(this.checkpoint&&scene.enableCheckpoint?scene.checkpoint:scene.spawn)};
      this.chains=(scene.chains||[]).map(def=>new Chain(def));this.climbingChain=null;
      this.stairways=(scene.stairways||[]).map(def=>new Stairway(def));this.walkingStairway=null;
      this.handholds=(scene.handholds||[]).map(def=>({...def}));this.holdingHandhold=null;
      this.playerSettings={...this.playerSettings,...scene.player};this.noiseSettings={...this.noiseSettings,...scene.noise};
      this.ammoCapacity=scene.player.ammoCapacity;this.ammo=this.ammoCapacity;this.maxHealth=scene.player.maxHealth;this.health=this.maxHealth;this.damageSlowDuration=scene.player.damageSlowDuration;this.healthRecoveryInterval=scene.player.healthRecoveryInterval;
      Object.assign(this.lighting,scene.lighting);this.lighting.factoryArea={...scene.lighting.factoryArea};this.lighting.fixedLights=scene.lighting.fixedLights.map(l=>({...l}));this.lighting.reset();
      this.streetLamps=scene.lighting.streetLamps.map(def=>new StreetLamp(def));
      this.readables=scene.readables||[];this.items=scene.items.map(def=>def.kind==='fuelPump'?new Items.FuelPump(def):new Items.ContinuousInteractionItem(def));
      this.dialogueTriggers=(scene.dialogueTriggers||[]).map(def=>({...def,fired:false,wasInside:false}));
      this.doors=scene.doors.map(def=>new Door({...def,onOpened:def.destinationScene?(g)=>g.travelToScene(def.destinationScene,def.destinationSpawn):null}));
      this.enemies=scene.enemies.map(def=>{
        const e=new Rampager(this,def.x,def.x+def.movement.leftOffset,def.x+def.movement.rightOffset);
        e.home={x:def.x,y:def.y};e.movement.home={...e.home};e.sleepPosition={...def.sleepPosition};
        for(const [k,v] of Object.entries(def))if(!['x','y','movement','perception','attack','patrol','sleepPosition'].includes(k))e[k]=v;
        Object.assign(e.movement,def.movement);e.idealRange=def.perception.idealSightRange;e.fov=def.perception.fieldOfView;e.minimumExposure=def.perception.minimumExposure;Object.assign(e.attack,def.attack);
        e.patrol=def.patrol?new Patrol(def.patrol.points,def.patrol.loop):null;e.reset();return e;
      });
      Object.assign(this.soundWaves,scene.sound);this.soundWaves.noiseSpeed=scene.noise.propagationSpeed;this.soundWaves.reset();
    }
    travelToScene(destination,spawnName=''){
      const id=destination.replace(/\.unity$/i,'').split('/').pop();if(!this.sceneConfig||!this.sceneConfig.scenes[id])throw new Error('Door destination missing: '+destination);
      const inventory=new Set(this.inventory),health=this.health,elapsed=this.elapsed,attempts=this.attempts;
      this.sceneId=id;this.checkpoint=false;this.reset(false);this.inventory=inventory;this.health=health;this.elapsed=elapsed;this.attempts=attempts;
      if(spawnName&&this.scene.spawns[spawnName])this.pos={...this.scene.spawns[spawnName]};
      this.refreshProgress();this.camera=this.pos.x+this.scene.camera.followOffset.x;this.cameraY=this.cameraTargetY();this.say(this.scene.label);
    }
    get cameraFollowPosition(){return this.climbingChain?{x:this.climbingChain.x,y:this.climbingChain.y-this.climbingChain.grip}:this.pos;}
    cameraTargetY(){const c=this.scene.camera,y=this.cameraFollowPosition.y+c.followOffset.y;return (c.limitVertical?clamp(y,Math.min(c.minimumY,c.maximumY),Math.max(c.minimumY,c.maximumY)):y)+this.mouseLookOffset.y;}
    updateMouseLook(input){
      this.mouseLookOffset={x:0,y:0};
      if(!this.scene||!this.scene.camera.mouseLookEnabled||input.mouseLookActive===false||!input.aim)return;
      const c=this.scene.camera,sensitivity=clamp(c.mouseLookSensitivity,0,.9),maximum=Math.max(0,c.maximumMouseOffset);
      const reference=this.cameraFollowPosition;
      const x=(input.aim.x+this.pos.x-reference.x)*sensitivity,y=(input.aim.y+this.pos.y-reference.y)*sensitivity,length=Math.hypot(x,y),ratio=length>maximum?maximum/length:1;
      this.mouseLookOffset={x:x*ratio,y:c.limitDownwardMouseOffset?Math.max(y*ratio,-Math.max(0,c.maximumDownwardMouseOffset??.5)):y*ratio};
    }
    get hasWaterGun(){return !this.scene||this.scene.startWithWaterGun||this.inventory.has('water_gun');}
    refreshProgress(){if(this.scene)this.blocks=this.scene.blocks.filter(b=>!b.disabledByItemId||!this.inventory.has(b.disabledByItemId)).map(b=>({...b}));}
    get climbing(){return !!(this.climbingLadder||this.climbingChain);}
    get activeFlightDuration(){return this.playerSettings.automaticRecoil?this.playerSettings.recoilDecayDuration+this.playerSettings.postRecoilFlightDuration:this.playerSettings.flightDuration;}
    releaseStairs(){if(this.walkingStairway){this.walkingStairway=null;this.stairReattachAt=this.time+.2;}}
    releaseHandhold(){if(this.holdingHandhold){this.handholdReattachAt=this.time+this.holdingHandhold.releaseCooldown;this.holdingHandhold=null;}}
    releaseChain(){if(this.climbingChain){this.velocity=this.climbingChain.velocity;this.climbingChain.occupied=false;this.climbingChain=null;}}
    ladderContains(l){return this.pos.x+.32>l.x&&this.pos.x-.32<l.x+l.w&&this.pos.y+this.height/2>=l.y-.03&&this.pos.y-this.height/2<=l.y+l.h+.03;}
    get height() { return this.crouched ? .85 : 1.65; }
    body() { return { x: this.pos.x - .32, y: this.pos.y - this.height / 2, w: .64, h: this.height }; }
    say(text) { this.monologue = text; this.monologueUntil = this.time + 5; }
    showDialogue(text,duration=5) { this.dialogue = text || ''; this.dialogueUntil = this.time + Math.max(.1,duration); }
    showInteractionMessage(text) { this.showDialogue(text); }
    get bottomDialogue() { return this.activeItem ? '正在互动 · ' + this.activeItem.name : this.time < this.dialogueUntil ? this.dialogue : ''; }
    clear(a, b) {
      const n = distance(a, b); if (n < .001) return true;
      const d = { x: (b.x - a.x) / n, y: (b.y - a.y) / n };
      return !this.blocks.some(r => r.blocksSight!==false && rayRect(a, d, r, n) < n - .03);
    }
    get violating(){return this.flying || this.time-this.shotAt<.18 || this.time<this.forbiddenUntil || !!this.activeItem;}
    get nearbyReadable(){return this.readables.filter(p=>distance(this.pos,p)<=p.range&&this.clear(this.pos,p)).sort((a,b)=>distance(this.pos,a)-distance(this.pos,b))[0];}
    openReadable(prop){if(!prop||this.dead||this.won||distance(this.pos,prop)>prop.range||!this.clear(this.pos,prop))return false;if(this.activeItem)this.activeItem.cancel();this.activeItem=null;this.interactionHeld=true;this.notifyInteraction();this.activeReadable=prop;return true;}
    closeReadable(){this.activeReadable=null;}
    get nearbyDoor(){return this.doors.filter(door=>door.inRange(this)).sort((a,b)=>distance(this.pos,a.interactionPosition)-distance(this.pos,b.interactionPosition))[0];}
    get nearbyStreetLamp(){return this.streetLamps.find(lamp=>lamp.inRange(this));}
    get nearbyItem(){return this.items.find(item=>item.inRange(this));}
    updateInteraction(held,dt){
      if(!held){if(this.activeItem)this.activeItem.cancel();this.activeItem=null;this.interactionHeld=false;return;}
      if(this.activeItem&&!this.activeItem.inRange(this)){this.activeItem.cancel();this.activeItem=null;}
      if(!this.interactionHeld&&!this.activeItem){this.activeItem=this.nearbyItem||null;if(this.activeItem)this.activeItem.begin(++this.interactionSession);}
      this.interactionHeld=true;
      if(this.activeItem&&this.activeItem.advance(this,dt))this.activeItem=null;
    }
    refreshDamageSpeed(){this.damageSlowExpiries=this.damageSlowExpiries.filter(expiry=>this.time<expiry-1e-8);this.damageSpeedMultiplier=.25**this.damageSlowExpiries.length;}
    recoverHealth(dt){
      this.refreshDamageSpeed();
      if(this.dead||this.won||this.health>=this.maxHealth||this.enemies.some(e=>e.canSee())||this.time<=this.lastHitAt){this.healthRecoveryElapsed=0;return;}
      this.healthRecoveryElapsed+=dt;
      while(this.healthRecoveryElapsed>=this.healthRecoveryInterval-1e-8&&this.health<this.maxHealth){this.healthRecoveryElapsed=Math.max(0,this.healthRecoveryElapsed-this.healthRecoveryInterval);this.health++;}
      if(this.health>=this.maxHealth)this.healthRecoveryElapsed=0;
    }
    takeMonsterHit(){
      if(this.dead||this.won||this.health<=0)return false;
      this.health--;this.lastHitAt=this.time;this.healthRecoveryElapsed=0;
      this.damageSlowExpiries.push(this.time+this.damageSlowDuration);this.refreshDamageSpeed();
      this.flying=false;this.climbingLadder=null;this.releaseChain();this.releaseHandhold();this.releaseStairs();this.flightNoiseStarted=this.wasUsingRecoil=false;this.pool={x:0,y:0};this.upwardAt=this.poolAt=-10;this.velocity.y=Math.min(0,this.velocity.y);
      if(this.activeItem)this.activeItem.cancel();this.activeItem=null;this.interactionHeld=true;
      this.dead=this.health<=0;this.say(`受到攻击！生命 ${this.health}/${this.maxHealth} · 移动速度 ${(this.damageSpeedMultiplier*100).toFixed(2)}%`);return true;
    }
    notifyInteraction(){this.forbiddenUntil=this.time+.2;this.interactionSequence++;}
    environmentBrightness(p){return this.lighting.environmentBrightness(p);}
    get illumination(){return this.lighting.illumination;}
    publishReport(report,radius=10){for(const e of this.enemies)if(e!==report.source&&distance(e,report.source)<=radius)e.receive(report);this.soundWaves.emitAid(report.source,radius);}
    noise(source,radius){this.noiseAt=this.time;this.soundWaves.emitNoise(source,radius);}
    shoot() {
      if(!this.hasWaterGun||this.ammo<=0||this.time<this.nextShot)return false;
      this.ammo--;this.ammoUIAt=this.time;this.refillStarted=false;
      this.shotSequence++;this.nextShot = this.time + this.playerSettings.shotInterval; this.shotAt = this.poolAt = this.time;
      this.releaseHandhold();this.releaseStairs();
      this.pool = this.playerSettings.automaticRecoil?{x:-this.aim.x*7,y:-this.aim.y*8}:this.aim.y > .999 ? { x: 0, y: 0 } : { x: -this.aim.x * 7, y: Math.max(0, -this.aim.y) * 8 };
      this.upwardAt=this.time;
      if(this.climbingChain){this.climbingChain.shot(this.aim);this.flying=false;this.grounded=false;this.velocity.y=0;}
      else{this.flying=true;this.climbingLadder=null;this.grounded=false;this.velocity.y=0;}
      const start = { x: this.pos.x + this.aim.x * .55, y: this.pos.y + this.aim.y * .55 };
      let range = this.playerSettings.shotRange;
      for (const r of this.blocks) range = Math.min(range, rayRect(start, this.aim, r, range));
      this.sprays.push({ start, end: { x: start.x + this.aim.x * range, y: start.y + this.aim.y * range }, at: this.time });
      for (const e of this.enemies) {
        const along = (e.x - start.x) * this.aim.x + (e.y - start.y) * this.aim.y;
        if (along > 0 && along < range + .2 && distance(e, { x: start.x + this.aim.x * along, y: start.y + this.aim.y * along }) < .75) {
          e.directHit(this.pos);
        }
      }
      return true;
    }
    move(dx, dy) {
      const epsilon=.0001,half=this.height/2;
      // Initial penetration is resolved to its nearest face, never automatically to the top.
      for(let pass=0;pass<8;pass++){
        let best=Infinity,correction=null;
        for(const b of this.blocks){
          if(this.pos.x+.32<=b.x+epsilon||this.pos.x-.32>=b.x+b.w-epsilon||this.pos.y+half<=b.y+epsilon||this.pos.y-half>=b.y+b.h-epsilon)continue;
          for(const [x,y] of [[b.x-this.pos.x-.32,0],[b.x+b.w-this.pos.x+.32,0],[0,b.y-this.pos.y-half],[0,b.y+b.h-this.pos.y+half]]){const depth=Math.abs(x||y);if(depth<best){best=depth;correction={x,y};}}
        }
        if(!correction)break;this.pos.x+=correction.x;this.pos.y+=correction.y;if(correction.x)this.velocity.x=0;if(correction.y)this.velocity.y=0;
      }
      const oldX=this.pos.x;let nextX=oldX+dx;
      for(const b of this.blocks){
        if(this.pos.y+half<=b.y+epsilon||this.pos.y-half>=b.y+b.h-epsilon)continue;
        if(dx>0&&oldX+.32<=b.x+epsilon&&nextX+.32>=b.x)nextX=Math.min(nextX,b.x-.32);
        else if(dx<0&&oldX-.32>=b.x+b.w-epsilon&&nextX-.32<=b.x+b.w)nextX=Math.max(nextX,b.x+b.w+.32);
      }
      this.pos.x=nextX;if(Math.abs(nextX-oldX-dx)>epsilon)this.velocity.x=0;
      const oldY=this.pos.y;let nextY=oldY+dy,verticalHit=false;this.grounded=false;
      for(const b of this.blocks){
        if(this.pos.x+.32<=b.x+epsilon||this.pos.x-.32>=b.x+b.w-epsilon)continue;
        if(dy<0&&oldY-half>=b.y+b.h-epsilon&&nextY-half<=b.y+b.h){nextY=Math.max(nextY,b.y+b.h+half);this.grounded=true;verticalHit=true;}
        else if(dy>0&&oldY+half<=b.y+epsilon&&nextY+half>=b.y){nextY=Math.min(nextY,b.y-half);verticalHit=true;}
      }
      this.pos.y=nextY;
      for(const r of this.climbing?[]:this.softRoofs){
        if(r===this.dropThroughRoof||this.pos.x+.3<=r.x||this.pos.x-.3>=r.x+r.w)continue;
        if(dy<0&&oldY-half>=r.y+r.h-epsilon&&this.pos.y-half<=r.y+r.h){this.pos.y=Math.max(this.pos.y,r.y+r.h+half);this.grounded=true;verticalHit=true;}
        else if(!this.flying&&!this.climbing&&dy<=0&&Math.abs(this.pos.y-half-r.y-r.h)<.015)this.grounded=true;
      }
      if(this.dropThroughRoof&&this.pos.y-half<this.dropThroughRoof.y+this.dropThroughRoof.h-.05)this.dropThroughRoof=null;
      if(verticalHit)this.velocity.y=0;
      if(!this.flying&&!this.climbing&&dy<=0)for(const b of this.blocks)if(this.pos.x+.3>b.x&&this.pos.x-.3<b.x+b.w&&Math.abs(this.pos.y-half-b.y-b.h)<.015){this.grounded=true;break;}
      if(this.grounded&&!(this.playerSettings.automaticRecoil&&this.time-this.shotAt<=this.activeFlightDuration))this.flying=false;
    }
    updateEnemy(e,dt){e.tick(dt);}
    tick(input, dt) {
      if(this.activeReadable){if(input.interact)this.closeReadable();return;}
      if (this.dead || this.won) return;
      if(input.testWaterGun){this.inventory.add('water_gun');this.ammoCapacity=this.ammo=15;this.refillElapsed=0;this.refillStarted=false;this.ammoUIAt=this.time;}
      if(input.interact && this.nearbyReadable && (!this.nearbyDoor || Math.abs(this.pos.x-this.nearbyReadable.x)<=Math.abs(this.pos.x-this.nearbyDoor.interactionPosition.x))){this.openReadable(this.nearbyReadable);return;}
      dt = Math.min(dt, .033); this.time += dt; this.elapsed += dt;this.refreshDamageSpeed();
      const x = input.x || 0, y = input.y || 0;this.horizontalIntent=x;
      this.updateMouseLook(input);
      const wasGrounded=this.grounded,wasFlying=this.flying;
      const wasSupported=this.grounded||this.climbing;
      for(const chain of this.chains)chain.idle(dt);
      if(input.broadcast)this.soundWaves.start();
      if (input.aim && Math.hypot(input.aim.x, input.aim.y) > .01) {
        const n = Math.hypot(input.aim.x, input.aim.y); this.aim = { x: input.aim.x / n, y: input.aim.y / n };
      }
      const previous = this.crouched;
      this.crouched = this.grounded && !!input.sneak;
      this.silentWalking=this.grounded && !!input.silentWalk;
      if (previous !== this.crouched) this.pos.y += this.crouched ? -.4 : .4;
      if (input.fire && this.time >= this.nextShot) this.shoot();
      if(this.walkingStairway&&input.jump)this.releaseStairs();
      let stairCaughtThisFrame=false;
      if(!this.walkingStairway&&!this.climbing&&!this.holdingHandhold&&!this.crouched&&!input.jump&&!input.fire&&this.time>=this.stairReattachAt){
        const feet={x:this.pos.x,y:this.pos.y-this.height/2};
        if(this.grounded&&!this.flying&&Math.abs(y)>.1)for(const stair of this.stairways){const entry=stair.entry(feet,y);if(entry){this.walkingStairway=entry;break;}}
        if(!this.walkingStairway&&(Math.abs(y)>.1||((this.flying||(!this.grounded&&Math.abs(this.velocity.y)>.1))&&Math.abs(x)>.1))){
          const candidates=this.stairways.map(s=>s.catchAt(feet)).filter(Boolean).sort((a,b)=>a.gap-b.gap);
          if(candidates.length){this.walkingStairway=candidates[0];this.walkingStairway.heading=y<-.1?-1:1;const p=this.walkingStairway.stair.pointAt(this.walkingStairway.distance);this.pos={x:p.x,y:p.y+this.height/2};stairCaughtThisFrame=true;}
        }
        if(this.walkingStairway){this.flying=false;this.pool={x:0,y:0};this.velocity={x:0,y:0};this.upwardAt=this.shotAt=this.poolAt=-10;}
      }
      if(this.holdingHandhold&&input.jump){this.releaseHandhold();this.velocity.y=this.playerSettings.jumpSpeed;}
      if(!this.walkingStairway&&!this.holdingHandhold&&!this.climbing&&!input.jump&&!input.fire&&this.time>=this.handholdReattachAt){
        this.holdingHandhold=this.handholds.find(h=>overlap({x:this.pos.x-.32,y:this.pos.y-this.height/2,w:.64,h:this.height},h))||null;
        if(this.holdingHandhold){this.flying=this.grounded=this.crouched=false;this.pool={x:0,y:0};this.velocity={x:0,y:0};this.upwardAt=this.shotAt=this.poolAt=-10;}
      }
      if(this.climbingChain&&input.jump){this.releaseChain();this.ladderReattachAt=this.time+.25;this.velocity.y+=this.playerSettings.jumpSpeed;}
      let dropping=false;
      if(input.jump&&y<-.1&&this.grounded){
        const roof=this.softRoofs.find(r=>this.pos.x+.3>r.x&&this.pos.x-.3<r.x+r.w&&Math.abs(this.pos.y-this.height/2-r.y-r.h)<.035);
        if(roof){this.dropThroughRoof=roof;this.grounded=this.flying=false;this.climbingLadder=null;this.ladderReattachAt=this.time+.25;this.velocity.y=-2;dropping=true;}
      }
      if(this.climbingLadder&&(!this.ladderContains(this.climbingLadder)||Math.abs(x)>.1||input.jump)){this.climbingLadder=null;this.ladderReattachAt=this.time+.25;if(input.jump&&!dropping)this.velocity.y=this.playerSettings.jumpSpeed;}
      if(!this.walkingStairway&&!this.holdingHandhold&&!this.climbing&&!this.flying&&!input.jump&&this.time>=this.ladderReattachAt&&Math.abs(y)>.1){
        this.climbingChain=this.chains.find(c=>c.contains(this.pos,this.height))||null;
        if(this.climbingChain)this.climbingChain.attach(this.pos);else this.climbingLadder=this.ladders.find(l=>this.ladderContains(l))||null;
      }
      if(this.climbingLadder){if(this.crouched){this.crouched=false;this.pos.y+=.4;}this.flying=this.grounded=false;this.velocity={x:0,y:0};}
      if(this.climbingChain){if(this.crouched){this.crouched=false;this.pos.y+=.4;}this.flying=this.grounded=false;this.velocity={x:0,y:0};}
      if (!this.walkingStairway&&!this.holdingHandhold&&!this.climbing&&!dropping&&(input.jump || input.launch || (!this.flying && y > 0))) {
        if (this.time - this.upwardAt < this.activeFlightDuration && !this.crouched) { this.flying = true; this.velocity.y = 0; }
        else if (input.jump && this.grounded && !this.crouched) this.velocity.y = this.playerSettings.jumpSpeed;
      }
      if (this.flying && this.time - this.shotAt > this.activeFlightDuration) { this.flying = false; this.say('停止射击，它就不再托住我。'); }
      const decay = clamp(1 - (this.time - this.poolAt) / Math.max(.01,this.playerSettings.recoilDecayDuration), 0, 1);
      const speed = this.flying ? this.playerSettings.hoverSpeed : (this.crouched || this.silentWalking) ? this.playerSettings.sneakSpeed : this.playerSettings.walkSpeed;
      this.velocity.x += clamp(x * speed - this.velocity.x, -22 * dt, 22 * dt);
      let mx = this.velocity.x, my,usingRecoil=this.flying&&!wasFlying&&decay>0&&Math.hypot(this.pool.x,this.pool.y)>0;
      const onStairs=!!this.walkingStairway;let stairFinished=false;
      if(onStairs){const state=this.walkingStairway,result=state.stair.advance({x:this.pos.x,y:this.pos.y-this.height/2},stairCaughtThisFrame?0:y,this.damageSpeedMultiplier*((this.crouched||this.silentWalking)?this.playerSettings.sneakSpeed/this.playerSettings.walkSpeed:1),dt,state,stairCaughtThisFrame?0:x);mx=(result.position.x-this.pos.x)/Math.max(.0001,dt);my=(result.position.y+this.height/2-this.pos.y)/Math.max(.0001,dt);stairFinished=result.finished;this.velocity={x:0,y:0};this.flying=false;}
      else if(this.holdingHandhold){mx=(this.holdingHandhold.position.x-this.pos.x)/Math.max(.0001,dt);my=(this.holdingHandhold.position.y-this.pos.y)/Math.max(.0001,dt);}
      else if(this.climbingChain){
        this.climbingChain.drive(x,y,dt,this.damageSpeedMultiplier);const target=this.climbingChain.position;mx=(target.x-this.pos.x)/Math.max(.0001,dt);my=(target.y-this.pos.y)/Math.max(.0001,dt);
      }else if(this.climbingLadder){
        const l=this.climbingLadder;mx=clamp(l.x+l.w/2-this.pos.x,-l.climbSpeed*dt,l.climbSpeed*dt)/Math.max(.0001,dt);my=(clamp(this.pos.y+y*l.climbSpeed*dt,l.y+this.height/2,l.y+l.h+this.height/2)-this.pos.y)/Math.max(.0001,dt);
      }else if (this.flying) {
        this.velocity.y = 0; my = y * this.playerSettings.verticalHoverSpeed;
        if(this.playerSettings.automaticRecoil){mx+=this.pool.x*decay;my+=this.pool.y*decay;usingRecoil=Math.hypot(this.pool.x*decay,this.pool.y*decay)>.000001;}
        else{if (x * this.pool.x * decay > 0){mx += this.pool.x * decay;usingRecoil=true;}if (y * this.pool.y * decay > 0){my += this.pool.y * decay;usingRecoil=true;}}
      } else { this.velocity.y -= this.playerSettings.gravity * dt; my = this.velocity.y; }
      if(!onStairs&&!this.climbingChain&&!this.holdingHandhold){mx*=this.damageSpeedMultiplier;my*=this.damageSpeedMultiplier;}
      let falling=!this.holdingHandhold&&!this.flying&&!this.climbing&&!this.grounded&&this.velocity.y<0;
      const oldX=this.pos.x,oldY=this.pos.y;if(onStairs){this.pos.x+=mx*dt;this.pos.y+=my*dt;this.grounded=true;if(stairFinished)this.releaseStairs();}else this.move(mx * dt, my * dt);
      if(this.climbingChain&&Math.hypot(this.pos.x-this.climbingChain.position.x,this.pos.y-this.climbingChain.position.y)>.05)this.climbingChain.stopAt(this.pos);
      this.movementVelocity=dt>0?{x:(this.pos.x-oldX)/dt,y:(this.pos.y-oldY)/dt}:{x:0,y:0};
      const landingFallDistance=this.fallDistance+Math.max(0,oldY-this.pos.y);
      falling=falling||(!this.holdingHandhold&&!this.grounded&&!this.flying&&!this.climbing&&my<0);this.highFall=false;
      if(falling){if(!this.fallActive){this.fallActive=true;this.fallDistance=0;this.fallSequence++;}this.fallDistance+=Math.max(0,oldY-this.pos.y);this.highFall=this.fallDistance>1.65;}
      else{this.fallActive=false;this.fallDistance=0;}
      if(!(this.grounded||this.climbing)||this.ammo>=this.ammoCapacity){this.refillElapsed=0;this.refillStarted=false;}
      else if(wasSupported){this.refillElapsed+=dt;while(this.refillElapsed>=this.playerSettings.groundRefillInterval-1e-8&&this.ammo<this.ammoCapacity){this.refillElapsed=Math.max(0,this.refillElapsed-this.playerSettings.groundRefillInterval);this.ammo++;if(!this.refillStarted){this.ammoUIAt=this.time;this.refillStarted=true;}}if(this.ammo>=this.ammoCapacity)this.refillElapsed=0;}
      this.movementBlocked=Math.abs(x)>.1&&Math.abs(this.pos.x-oldX)<Math.abs(mx*dt)*.2;
      if(!this.flying)this.flightNoiseStarted=this.wasUsingRecoil=false;
      else{
        if((usingRecoil&&!this.wasUsingRecoil)||(this.flightNoiseStarted&&this.time>=this.nextFlightNoise)){
          this.noise(this.pos,this.noiseSettings.footstepRadius);this.flightNoiseStarted=true;
          this.nextFlightNoise=this.time+this.noiseSettings.footstepInterval;
        }
        this.wasUsingRecoil=usingRecoil;
      }
      if(!wasGrounded && this.grounded && !this.climbing && my<0){
        if(landingFallDistance>1.65){
          this.noise(this.pos,this.noiseSettings.footstepRadius);
          this.footstepAt=this.time+this.noiseSettings.footstepInterval;
        }
      }
      else if (this.grounded && (onStairs?Math.hypot(mx,my)>.1:Math.abs(x)>.1) && !this.crouched && !this.silentWalking && this.time > this.footstepAt) {
        this.noise(this.pos, this.noiseSettings.footstepRadius); this.footstepAt = this.time + this.noiseSettings.footstepInterval;
      }
      if ((!this.scene || this.scene.enableCheckpoint) && this.pos.x > (this.scene?this.scene.checkpoint.x-.6:18) && !this.checkpoint) { this.checkpoint = true; this.say('普通行走可以混入其中。被目击射击或互动会立即激怒。搜索背包时，被看见后要及时松开 E。'); }
      this.updateInteraction(!!input.interactHeld,dt);
      const lamp=input.interact?this.nearbyStreetLamp:null;
      if(lamp)lamp.interact(this);
      if (input.interact && !lamp && !this.nearbyItem && !this.activeItem) {
        if ((!this.scene || this.scene.legacyEvidenceEnabled) && !this.hasClue && distance(this.pos, { x: 31.5, y: .8 }) < 1.5) { this.notifyInteraction();this.hasClue = true; this.say('一个孩子的名字，一个地址。这字迹为什么这么熟悉？'); }
        const door=this.nearbyDoor;if(door)door.tryInteract(this);
      }
      for(const lamp of this.streetLamps)lamp.tick(dt);
      for(const door of this.doors)if(door.tryAutoTravel(this))return;
      for(const trigger of this.dialogueTriggers){
        if(trigger.triggerOnce && trigger.fired)continue;
        const inside=this.pos.x+.32>trigger.x && this.pos.x-.32<trigger.x+trigger.w && this.pos.y+this.height/2>trigger.y && this.pos.y-this.height/2<trigger.y+trigger.h;
        if(inside && !trigger.wasInside){this.showDialogue(trigger.message,trigger.displayDuration);trigger.fired=true;}
        trigger.wasInside=inside;
      }
      for(const trigger of this.scene?.towerTriggers||[]){
        if(!this.inventory.has(trigger.progressId)&&Math.abs(this.pos.x-trigger.x)<=trigger.horizontalRange&&this.pos.y-this.height/2>trigger.top&&this.soundWaves.startAt(trigger.source))this.inventory.add(trigger.progressId);
      }
      this.soundWaves.tick(dt);
      for (const e of this.enemies) this.updateEnemy(e, dt);
      this.recoverHealth(dt);
      if(this.scene){const c=this.scene.camera,blend=c.followSpeed<=0?1:1-Math.exp(-c.followSpeed*dt);this.camera+=(this.cameraFollowPosition.x+c.followOffset.x+this.mouseLookOffset.x-this.camera)*blend;this.cameraY+=(this.cameraTargetY()-this.cameraY)*blend;}
      else this.camera += (clamp(this.pos.x + 3, 12, 28) - this.camera) * (1 - Math.exp(-4 * dt));
      this.sprays = this.sprays.filter(s => this.time - s.at < .16);
      this.noises = this.noises.filter(s => this.time - s.at < .6);
    }
  }
  root.Recoil = { Game, rayRect, clamp, distance };
  if (typeof module !== 'undefined') module.exports = root.Recoil;
})(globalThis);
