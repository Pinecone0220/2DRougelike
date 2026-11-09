using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using System.Collections;
using System.IO;
using System.Linq;

public class DemoDirector:MonoBehaviour {
 string output, ffmpeg; int shot, frame; bool recording; float elapsed, nextJump, nextAttack; Vector3 lastPosition;
 System.Diagnostics.Process encoder; Texture2D texture; player hero; Keyboard keyboard;
 [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
 static void Boot(){ if(System.Environment.GetEnvironmentVariable("DEMO_OUTPUT")!=null){var g=new GameObject("DemoDirector");DontDestroyOnLoad(g);g.AddComponent<DemoDirector>();}}
 IEnumerator Start(){
  output=System.Environment.GetEnvironmentVariable("DEMO_OUTPUT"); ffmpeg=System.Environment.GetEnvironmentVariable("DEMO_FFMPEG");
  Application.runInBackground=true; QualitySettings.vSyncCount=0; Application.targetFrameRate=30; Screen.SetResolution(1280,720,false);
  keyboard=InputSystem.AddDevice<Keyboard>(); keyboard.MakeCurrent();
  yield return null; yield return null;
  Time.captureFramerate=30;
  for(shot=0;shot<4;shot++){
   if(shot>0){GameSessionData.ResetSession();Time.timeScale=1;SceneManager.LoadScene("Stage "+shot);yield return null;yield return null;}
   hero=FindFirstObjectByType<player>(); elapsed=0; nextJump=3.5f;nextAttack=0;frame=0;
   if(hero!=null)lastPosition=hero.transform.position;
   var psi=new System.Diagnostics.ProcessStartInfo(ffmpeg,"-y -f rawvideo -pixel_format rgb24 -video_size 1280x720 -framerate 30 -i pipe:0 -vf vflip -c:v libx264 -preset fast -crf 18 -pix_fmt yuv420p \""+Path.Combine(output,"shot"+shot+".mp4")+"\"") {UseShellExecute=false,RedirectStandardInput=true,RedirectStandardError=true,CreateNoWindow=true};
   encoder=System.Diagnostics.Process.Start(psi);encoder.ErrorDataReceived+=(s,e)=>{};encoder.BeginErrorReadLine();
   texture=new Texture2D(1280,720,TextureFormat.RGB24,false);recording=true;
   int count=(shot==0?5:shot==3?25:20)*30;
   while(frame<count){yield return new WaitForEndOfFrame();texture.ReadPixels(new Rect(0,0,1280,720),0,0,false);texture.Apply(false);var bytes=texture.GetRawTextureData<byte>().ToArray();encoder.StandardInput.BaseStream.Write(bytes,0,bytes.Length);frame++;elapsed=frame/30f;
    if(frame%150==0){File.WriteAllBytes(Path.Combine(output,"preview_"+shot+"_"+frame+".png"),texture.EncodeToPNG());Debug.Log("DEMO shot "+shot+" frame "+frame+(hero!=null?" pos "+hero.transform.position:""));}
   }
   recording=false;InputSystem.QueueStateEvent(keyboard,new KeyboardState());encoder.StandardInput.Close();encoder.WaitForExit();Destroy(texture);
  }
  File.WriteAllText(Path.Combine(output,"done.txt"),"Complete");Application.Quit();
 }
 void Update(){
  if(!recording||hero==null||!hero.enabled)return;
  // Choreography is limited to the recording build; game assets remain untouched.
  if(hero.curHp<20)hero.curHp=hero.maxHp;
  if(hero.skill_UI!=null&&hero.skill_UI.activeSelf){hero.skill_UI.SetActive(false);Time.timeScale=1;}
  var attack=hero.GetComponent<AttackManager>();var rb=hero.GetComponent<Rigidbody2D>();
  Vector3 target=hero.transform.position+Vector3.right*5;
  if(shot==3){var boss=FindFirstObjectByType<BossManager>();if(boss!=null)target=boss.transform.position;}
  else {var enemy=FindObjectsByType<EnemyManager>(FindObjectsSortMode.None).Where(e=>e.curhp>0).OrderBy(e=>Vector2.Distance(e.transform.position,hero.transform.position)).FirstOrDefault();if(enemy!=null)target=enemy.transform.position;}
  float dx=target.x-hero.transform.position.x;float dy=target.y-hero.transform.position.y;
  bool near=Mathf.Abs(dx)<(shot==3?1.8f:1.0f)&&Mathf.Abs(dy)<2;
  bool dash=!near&&Mathf.Abs(dx)>4&&frame%150==100;
  InputSystem.QueueStateEvent(keyboard,near?new KeyboardState():dash?new KeyboardState(dx>0?Key.RightArrow:Key.LeftArrow,Key.C):new KeyboardState(dx>0?Key.RightArrow:Key.LeftArrow));
  if(near&&elapsed>nextAttack){
   hero.transform.localRotation=Quaternion.Euler(0,dx>=0?0:180,0);
   if(!attack.isAttacking)attack.SendMessage("StartAttack");
   else if(attack.canCombo){attack.comboInput=true;hero.GetComponent<Animator>().SetTrigger("Attack");}
   nextAttack=elapsed+0.25f;
  }
  if(elapsed>nextJump&&!attack.isAttacking&&hero.isGround&&(!near||dy>1)){
   rb.linearVelocity=new Vector2(rb.linearVelocity.x,0);rb.AddForce(Vector2.up*hero.jmp_spd,ForceMode2D.Impulse);hero.GetComponent<AudioSource>().PlayOneShot(hero.jump);nextJump=elapsed+2.3f;
  }
 }
}
