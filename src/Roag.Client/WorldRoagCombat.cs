using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Roag.Core.World;

namespace Roag.Client;

internal sealed partial class WorldGame
{
    private string _roagAim="move";
    private bool _roagVow;
    private KeyboardState _roagKeyboard;
    private void BuildRoagControls(int w,int h)
    {
        string[] kinds=["move","basic_attack","sweeping_slash","dash","wait","revive"];
        string[] labels=["Move","Basic attack","Special","Movement skill","Wait","Rescue (medicine)"];
        var active=_session.RoagFighters().FirstOrDefault(a=>a.Ready);
        int width=(_map.Width-16)/3;
        for(int i=0;i<kinds.Length;i++)ControlCache.Add("roag:"+kinds[i],labels[i],new(_map.X+i%3*(width+8),h-94+i/3*44,width,36),true,_roagAim==kinds[i],"Choose an action, then a direction on the map.","",null,"",-1,null,false);
        if(active!=null)_notice=active.Name+" / "+_session.RoagClassName(active.Class)+" — "+(_roagAim=="move"?"click an adjacent tile":_roagAim.Replace('_',' '));
    }
    private bool ActivateRoagControl(string id)
    {
        if(!_session.Plan.RoagNative)return false;
        if(id=="pause")
        {
            if(_session.State.RoagCombat)CommitRoag(new("wait"));
            else { _session.Paused=false;Execute(new(WorldCommandKind.Wait,Amount:5)); }
            return true;
        }
        if(!id.StartsWith("roag:",StringComparison.Ordinal))return false;
        _roagAim=id[5..];if(_roagAim=="wait")CommitRoag(new("wait"));return true;
    }
    private void CommitRoag(RoagAim aim)
    {
        var result=_session.SubmitRoag(aim);_notice=result.Message;
        if(result.Success){StopTravel();_roagAim="move";_terrainLayerDirty=true;}
    }
    private void RoagClick(Point pointer)
    {
        if(!_map.Contains(pointer))return;
        var world=_camera.ScreenToWorld(new(pointer.X,pointer.Y));
        var p=new WorldPosition((int)Math.Floor(world.X),(int)Math.Floor(world.Y),_session.State.Position.Floor);
        var active=_session.RoagFighters().FirstOrDefault(a=>a.Ready);if(active==null)return;
        if(_roagAim=="revive")
        {
            var target=_session.RoagFighters().FirstOrDefault(a=>a.Friendly&&a.Health<=0&&a.Position==p);
            if(target!=null)CommitRoag(new("revive",Target:target.Id));else _notice="Choose a downed companion beside the active member.";
            return;
        }
        int dx=Math.Sign(p.X-active.Position.X),dy=Math.Sign(p.Y-active.Position.Y);
        if(_roagAim is "move" or "dash")
        {if(Math.Abs(p.X-active.Position.X)>=Math.Abs(p.Y-active.Position.Y))dy=0;else dx=0;}
        CommitRoag(new(_roagAim,dx,dy,Vow:_roagVow));
    }
    private void UpdateRoagKeyboard()
    {
        if(!_session.Plan.RoagNative)return;
        var keyboard=Keyboard.GetState();
        bool Press(Keys key)=>keyboard.IsKeyDown(key)&&_roagKeyboard.IsKeyUp(key);
        if(InputFocused&&_panel.Length==0)
        {
            if(_session.State.RoagCombat)
            {
                if(Press(Keys.F))_roagAim="basic_attack";if(Press(Keys.R))_roagAim="sweeping_slash";if(Press(Keys.X))_roagAim="dash";
                if(Press(Keys.P))_roagVow=!_roagVow;if(Press(Keys.Escape))_roagAim="move";
                if(Press(Keys.Space)||Press(Keys.OemPeriod))CommitRoag(new("wait"));
                int dx=Press(Keys.D)||Press(Keys.Right)?1:Press(Keys.A)||Press(Keys.Left)?-1:0;
                int dy=Press(Keys.S)||Press(Keys.Down)?1:Press(Keys.W)||Press(Keys.Up)?-1:0;
                if(Press(Keys.Q)){dx=-1;dy=-1;}if(Press(Keys.E)){dx=1;dy=-1;}
                if(Press(Keys.Z)){dx=-1;dy=1;}if(Press(Keys.C)){dx=1;dy=1;}
                if(dx!=0||dy!=0)CommitRoag(new(_roagAim,dx,dy,Vow:_roagVow));
            }
            else
            {
                int dx=Press(Keys.D)||Press(Keys.Right)?1:Press(Keys.A)||Press(Keys.Left)?-1:0;
                int dy=Press(Keys.S)||Press(Keys.Down)?1:Press(Keys.W)||Press(Keys.Up)?-1:0;
                if(dx!=0||dy!=0){_session.Paused=false;Execute(new(WorldCommandKind.Move,_session.State.Position.Offset(dx,dy)));}
                if(Press(Keys.Space)||Press(Keys.OemPeriod))Execute(new(WorldCommandKind.Wait,Amount:5));
            }
        }
        _roagKeyboard=keyboard;
    }
    private void DrawRoagCombat()
    {
        foreach(var warning in _session.RoagTelegraphs())foreach(var cell in warning.Cells)
        {
            if(!_session.Visible.Contains(cell))continue;
            var r=TileRect(cell);FillClipped(new(r.X,r.Y,r.Width,2),new Color(240,112,78));FillClipped(new(r.X,r.Bottom-2,r.Width,2),new Color(240,112,78));
            FillClipped(new(r.X,r.Y,2,r.Height),new Color(240,112,78));FillClipped(new(r.Right-2,r.Y,2,r.Height),new Color(240,112,78));
        }
        int y=_side.Y+12;
        Text("ROAG / PARTY TACTICS",_side.X+12,y,Gold);y+=28;
        foreach(var fighter in _session.RoagFighters().Where(a=>a.Friendly))
        {
            Text((fighter.Ready?"> ":"  ")+Short(fighter.Name,24),_side.X+12,y,fighter.Ready?Gold:Ink);y+=20;
            Text($"{fighter.Health}/{fighter.MaxHealth} HP"+(fighter.Health<=0?" / DOWNED":""),_side.X+20,y,fighter.Health<=0?new Color(240,112,78):Muted);y+=28;
            if(fighter.Ready)
            {
                var r=TileRect(fighter.Position);FillClipped(new(r.X,r.Bottom-3,r.Width,3),Gold);
            }
        }
        Text("F attack / R special / X move",_side.X+12,y+10,Muted);
        Text("Space waits. P toggles Vow.",_side.X+12,y+30,Muted);
        var active=_session.RoagFighters().FirstOrDefault(a=>a.Ready);if(active==null)return;
        var mouse=Mouse.GetState();if(!_map.Contains(mouse.Position))return;
        var point=_camera.ScreenToWorld(new(mouse.X,mouse.Y));int dx=Math.Sign((int)Math.Floor(point.X)-active.Position.X),dy=Math.Sign((int)Math.Floor(point.Y)-active.Position.Y);
        if(_roagAim is "move" or "dash"){if(Math.Abs(point.X-active.Position.X)>=Math.Abs(point.Y-active.Position.Y))dy=0;else dx=0;}
        foreach(var cell in _session.RoagPreview(new(_roagAim,dx,dy)))if(_session.Visible.Contains(cell))FillClipped(TileRect(cell),new Color(113,187,195)*.2f);
    }
}
