(function(root){
  'use strict';
  const SceneConfig=typeof module!=='undefined'?require('./scene-lighting-config.js'):root.RecoilSceneLightingConfig;
  const clamp=(value,min,max)=>Math.max(min,Math.min(max,value));
  const smooth=t=>t*t*(3-2*t);
  class SceneLighting {
    constructor(game){
      this.game=game;this.fullyLitBrightness=.45;this.outdoorAmbient=.28;
      this.playerLightRadius=5.4;this.playerLightIntensity=.48;
      this.swarmFlickerIntensity=.45;this.swarmFlickerPeriod=1.4;this.swarmFlashDuration=.45;
      this.fixedLights=[{x:18.6,y:2.3,radius:2,intensity:.22},{x:25,y:5.4,radius:2.2,intensity:.26},{x:37.5,y:2.8,radius:2.4,intensity:.35}];
      Object.assign(this,SceneConfig);this.factoryArea={...SceneConfig.factoryArea};this.fixedLights=SceneConfig.fixedLights.map(light=>({...light}));
      this.reset();
    }
    reset(){this.lightSelectionTime=undefined;this.alarmWasActive=false;this.alarmStartedAt=0;this.sampleKey=null;}
    get swarmAmbient(){
      const alarm=!!(this.game.soundWaves&&this.game.soundWaves.redAlert);
      if(alarm&&!this.alarmWasActive)this.alarmStartedAt=this.game.time;
      this.alarmWasActive=alarm;if(!alarm)return 0;
      const period=Math.max(.1,this.swarmFlickerPeriod),phase=((this.game.time-this.alarmStartedAt)%period+period)%period;
      const duration=Math.min(period,Math.max(.05,this.swarmFlashDuration));
      const rise=clamp(phase/(duration*.25),0,1),fade=clamp((phase-duration*.35)/(duration*.65),0,1);
      return this.swarmFlickerIntensity*smooth(rise)*(1-smooth(fade));
    }
    isIndoors(p){const a=this.factoryArea;return !!(a&&a.enabled&&p.x>=a.minX&&p.x<=a.maxX&&p.y>=a.minY&&p.y<=a.maxY);}
    get selectedLights(){
      const g=this.game,cx=Number.isFinite(g.camera)?g.camera:g.pos.x,cy=Number.isFinite(g.cameraY)?g.cameraY:g.pos.y;
      if(this.lightSelectionTime===g.time&&this.lightSelectionX===cx&&this.lightSelectionY===cy)return this.lightSelection;
      this.lightSelectionTime=g.time;this.lightSelectionX=cx;this.lightSelectionY=cy;
      this.lightSelection=this.fixedLights.filter(l=>l.enabled!==false).sort((a,b)=>((a.x-cx)**2+(a.y-cy)**2-a.radius**2)-((b.x-cx)**2+(b.y-cy)**2-b.radius**2)).slice(0,31);
      return this.lightSelection;
    }
    environmentBrightness(p){
      let brightness=this.isIndoors(p)?this.swarmAmbient:this.outdoorAmbient;
      for(const light of this.selectedLights){
        const t=clamp(Math.hypot(p.x-light.x,p.y-light.y)/Math.max(.001,light.radius),0,1);
        brightness=Math.max(brightness,light.intensity*(1-smooth(t)));
      }
      if(this.game.streetLamps&&this.game.streetLamps.some(lamp=>lamp.contains(p)))brightness=1;
      return brightness;
    }
    get illumination(){
      const g=this.game,half=g.crouched?.35:.65;
      const key=[g.time,g.pos.x,g.pos.y,half,!!(g.soundWaves&&g.soundWaves.redAlert),this.fullyLitBrightness,this.outdoorAmbient,this.factoryArea.enabled,this.factoryArea.minX,this.factoryArea.maxX,this.factoryArea.minY,this.factoryArea.maxY];
      if(this.sampleKey&&key.every((value,i)=>value===this.sampleKey[i]))return this.sampledIllumination;
      let brightness=0;for(let i=-1;i<=1;i++)brightness+=this.environmentBrightness({x:g.pos.x,y:g.pos.y+half*i});
      this.sampledIllumination=.4+.6*clamp(brightness/(3*Math.max(.001,this.fullyLitBrightness)),0,1);if(g.streetLamps&&g.streetLamps.some(lamp=>lamp.contains(g.pos)))this.sampledIllumination=1;this.sampleKey=key;
      return this.sampledIllumination;
    }
  }
  root.SceneLighting=SceneLighting;if(typeof module!=='undefined')module.exports=SceneLighting;
})(globalThis);
