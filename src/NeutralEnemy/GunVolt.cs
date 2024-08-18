using System;
using System.Collections.Generic;

namespace MMXOnline;
public class GunVolt: NeutralEnemy {
	public static Weapon getWeapon() { return new Weapon(WeaponIds.GunVolt, 159); }
    public GunVolt(Player owner, Point pos, int xDir, ushort? netId, bool isLocal, bool addToLevel = true) : base(pos, netId, isLocal, addToLevel){
        enemyId = (int)EnemyIds.GunVolt;
        changeSprite(getSprite("_idle"), true);
        wSize = 42;
        hSize = 58;
        health = 16;
        maxHealth = 16;
    }
	// Sprite change override.
	/*public virtual string getSprite(string spriteName) {
		if (spriteName is null or "") {
			return "";
		}
		return "gunvolt" + spriteName;
	}*/

	// For terrain collision.
	public override Collider? getTerrainCollider() {
		return new Collider(
	        new Rect(0f, 0f, 14, 18).getPoints(),
    	   	false, this, false, false,
        	HitboxFlag.Hurtbox, Point.zero
    	);
	}
}

public class GunVoltSparkProj : Projectile {
    public GunVoltSparkProj(Weapon weapon, Point pos, int xDir, Player player, ushort netProjId, bool rpc = false) :
    base(weapon, pos, xDir, 0, 4, player, "gunvolt_spark", Global.defFlinch, 0.5f, netProjId, player.ownedByLocalPlayer){
        projId = (int)ProjIds.GunVoltSpark;
		collider.wallOnly = true;
		maxTime = 500;
		destroyOnHit = false;
    }
	public override void update() {
		base.update();
		var collideData = Global.level.checkCollisionActor(this, xDir, 0, vel);
		if (collideData != null && collideData.hitData != null) {
			playSound("dingX2");
			vel.y = -200;
			vel.x = 0;
			new Anim(pos, "sonicslicer_sparks", xDir, null, true);
			//RPC.actorToggle.sendRpc(netId, RPCActorToggleType.SonicSlicerBounce);
		}
		int velYSign = MathF.Sign(vel.y);
		if (velYSign != 0) {
			collideData = Global.level.checkCollisionActor(this, 0, velYSign, vel);
			if (collideData != null && collideData.hitData != null) {
				playSound("dingX2");
				vel.y = 0;
				vel.x = 400;
				new Anim(pos, "sonicslicer_sparks", xDir, null, true);
			}
		}
		if (collideData == null){ vel.x = 400 * xDir; vel.y = 200; }
		/*var wall = Global.level.checkCollisionActor(this, 0, 0);
		if (wall != null && wall.gameObject is Wall) {
			vel.x = 0;
			vel.y = 100;
		} else {
			vel.x = 400 * xDir;
			if (grounded) {
				vel.y = 0;
			} else {
				vel.y = 100;

			}
		}*/
	}
}
public class GunVoltTorpedoProj : Projectile {
    public GunVoltTorpedoProj(Weapon weapon, Point pos, int xDir, int type, Player player, ushort netProjId, bool rpc = false) :
    base(weapon, pos, xDir, 200, 4, player, "gunvolt_torpedo", Global.defFlinch, 0.5f, netProjId, player.ownedByLocalPlayer){
        projId = (int)ProjIds.GunVoltTorpedo;
        isShield = true;
		maxDistance = 250f;
		destroyOnHit = true;
		fadeSprite = "explosion";
		fadeSound = "explosion";
        
    }
}