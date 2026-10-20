(function(root){
  'use strict';
  class ContinuousInteractionItem {
    constructor({id,name,description='',duration=1.5,resultId,resultMessage,x,y,range=1.5}){Object.assign(this,{id,name,description,duration,resultId,resultMessage,x,y,range});this.reset();}
    reset(){this.progress=0;this.completed=false;this.session=0;}
    inRange(g){return !this.completed&&!g.inventory.has(this.resultId)&&Math.hypot(g.pos.x-this.x,g.pos.y-this.y)<=this.range&&g.clear(g.pos,this);}
    begin(session){this.session=session;this.progress=0;}
    cancel(){this.progress=0;this.session=0;}
    advance(g,dt){this.progress+=dt;if(this.progress<this.duration)return false;this.progress=this.duration;this.completed=true;g.inventory.add(this.resultId);g.showInteractionMessage(this.resultMessage);return true;}
  }
  class Backpack extends ContinuousInteractionItem {
    constructor(x=28.8,y=.55){super({id:'backpack',name:'员工背包',description:'持续搜索，获取员工门禁卡',duration:1.5,resultId:'employee_access_card',resultMessage:'获得员工门禁卡。',x,y});}
  }
  class FuelPump extends ContinuousInteractionItem {
    constructor(definition){super(definition);this.kind='fuelPump';this.stationId=definition.stationId;this.screen=definition.screen;this.nozzle=definition.nozzle;this.pickupPoseDuration=definition.pickupPoseDuration||.8;this.pickupUntil=-Infinity;}
    advance(g,dt){if(this.completed)return false;this.progress=this.duration;this.completed=true;g.notifyInteraction();g.inventory.add(this.resultId);this.pickupUntil=g.time+this.pickupPoseDuration;for(const pump of g.items||[this])if(pump.kind==='fuelPump'&&pump.stationId===this.stationId){pump.progress=pump.duration;pump.completed=true;g.inventory.add(pump.resultId);}return true;}
  }
  root.ContinuousItems={ContinuousInteractionItem,Backpack,FuelPump};if(typeof module!=='undefined')module.exports=root.ContinuousItems;
})(globalThis);
