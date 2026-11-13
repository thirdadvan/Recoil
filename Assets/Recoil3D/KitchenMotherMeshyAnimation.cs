using UnityEngine;

[DisallowMultipleComponent,DefaultExecutionOrder(850)]
public sealed class KitchenMotherMeshyAnimation : MonoBehaviour
{
 public Animator animator;
 public KitchenMemoryController story;
 Vector3 previous;
 string current="";
 void OnEnable(){previous=transform.position;current="";}
 void Update(){
  if(!animator||!story)return;
  var delta=transform.position-previous;previous=transform.position;
  string state=story.MotherLeaving&&!story.BedroomClosed?"Walk":"Cook";
  if(state!=current){animator.CrossFadeInFixedTime(state,.22f);current=state;}
  animator.applyRootMotion=false;
  if(state=="Walk"&&delta.sqrMagnitude>.000001f){
   var direction=new Vector3(delta.x,0,delta.z);
   if(direction.sqrMagnitude>.000001f)transform.rotation=Quaternion.RotateTowards(transform.rotation,Quaternion.LookRotation(direction),240*Time.deltaTime);
  }
 }
}
