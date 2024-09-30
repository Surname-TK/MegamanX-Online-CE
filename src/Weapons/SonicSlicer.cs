using System;
using System.Collections.Generic;
using System.Globalization;

namespace MMXOnline;

public class SonicSlicer : Weapon {
	public SonicSlicer() : base() {
		shootSounds = new string[] { "sonicSlicer", "sonicSlicer", "sonicSlicer", "sonicSlicerCharged" };
		rateOfFire = 0.25f;
		index = (int)WeaponIds.SonicSlicer;
		weaponBarBaseIndex = 13;
		weaponBarIndex = weaponBarBaseIndex;
		weaponSlotIndex = 13;
		killFeedIndex = 24;
		weaknessIndex = (int)WeaponIds.CrystalHunter;
		damage = "2/4";
		effect = "Bounces on Wall. Breaks W.Sponge Shield.";
		hitcooldown = "0/0.25";
		Flinch = "0/26";
	}

	public override void getProjectile(Point pos, int xDir, Player player, float chargeLevel, ushort netProjId) {
		if (chargeLevel < 3) {
			new SonicSlicerStart(this, pos, xDir, player, netProjId);
		} else {
			new Anim(pos, "sonicslicer_charge_start", xDir, null, true);
			player.setNextActorNetId(netProjId);
			new SonicSlicerProjCharged(this, pos, 0, player, player.getNextActorNetId(true));
			new SonicSlicerProjCharged(this, pos, 1, player, player.getNextActorNetId(true));
			new SonicSlicerProjCharged(this, pos, 2, player, player.getNextActorNetId(true));
			new SonicSlicerProjCharged(this, pos, 3, player, player.getNextActorNetId(true));
			new SonicSlicerProjCharged(this, pos, 4, player, player.getNextActorNetId(true));
		}
	}
}

public class SonicSlicerStart : Projectile {
	public SonicSlicerStart(Weapon weapon, Point pos, int xDir, Player player, ushort netProjId, bool rpc = false) :
		base(weapon, pos, xDir, 0, 1, player, "sonicslicer_start", 0, 0, netProjId, player.ownedByLocalPlayer) {
		projId = (int)ProjIds.SonicSlicerChargedStart;
		if (rpc) {
			rpcCreate(pos, player, netProjId, xDir);
		}
	}

	public override void update() {
		base.update();
		if (sprite.isAnimOver()) {
			if (ownedByLocalPlayer) {
				new SonicSlicerProj(weapon, pos, xDir, 0, damager.owner, damager.owner.getNextActorNetId(), rpc: true);
				new SonicSlicerProj(weapon, pos, xDir, 1, damager.owner, damager.owner.getNextActorNetId(), rpc: true);
			}
			destroySelf();
		}
	}
}

public class SonicSlicerProj : Projectile {
	public Sprite twin;
	public float Curve = 1;
	int type;
	public SonicSlicerProj(Weapon weapon, Point pos, int xDir, int type, Player player, ushort netProjId, bool rpc = false) :
		base(weapon, pos, xDir, 0, 1, player, "sonicslicer_proj", 0, 0, netProjId, player.ownedByLocalPlayer) {
		maxTime = 2;
		this.type = type;
		collider.wallOnly = true;
		projId = (int)ProjIds.SonicSlicer;

		twin = new Sprite("sonicslicer_twin");

		if (time > 0.25f) {
			vel.x = 200;
			vel.y = 800;
			if (type == 1) {
				vel.x *= 1.25f;
				frameIndex = 1;
			}
		}

		if (rpc) {
			rpcCreate(pos, player, netProjId, xDir, (byte)type);
		}
	}

	public override void update() {
		base.update();
		if (time > 0.25f) {
			Curve -= 0.05f;
			if (type == 0) {
				vel.x = 150 * xDir;
				vel.y = (Curve * 30) + 10;
			} else {
				vel.x = 200 * xDir;
				vel.y = (Curve * 30);
			}
		}

		var collideData = Global.level.checkTerrainCollisionOnce(this, xDir, 0, vel);
		if (collideData != null && collideData.hitData != null) {
			playSound("dingX2");
			xDir *= -1;
			time -= 0.5f;
			vel.x *= -1;
			new Anim(pos, "sonicslicer_sparks", xDir, null, true);
			//RPC.actorToggle.sendRpc(netId, RPCActorToggleType.SonicSlicerBounce);
		}

		int velYSign = MathF.Sign(vel.y);
		if (velYSign != 0) {
			collideData = Global.level.checkTerrainCollisionOnce(this, 0, velYSign, vel);
			if (collideData != null && collideData.hitData != null) {
				playSound("dingX2");
				vel.y *= -1;
				Curve *= -1;
				new Anim(pos, "sonicslicer_sparks", xDir, null, true);
				//RPC.actorToggle.sendRpc(netId, RPCActorToggleType.SonicSlicerBounce);
			}
		}
	}

	public override void render(float x, float y) {
		base.render(x, y);
		float ox = -vel.x * Global.spf * 3;
		float oy = -vel.y * Global.spf * 3;
		twin.draw(frameIndex, pos.x + x + ox, pos.y + y + oy, 1, 1, null, 0.5f, 1, 1, zIndex);
	}
}

public class SonicSlicerProjCharged : Projectile {
	public Point dest;
	public bool fall;
	public int Type;
	public SonicSlicerProjCharged(Weapon weapon, Point pos, int type, Player player, ushort netProjId, bool rpc = false) :
		base(weapon, pos, 1, 0, 2, player, "sonicslicer_charged", Global.defFlinch, 0.02f, netProjId, player.ownedByLocalPlayer) {
		fadeSprite = "sonicslicer_charged_fade";
		Type = type;
		maxTime = 1.5f;
		projId = (int)ProjIds.SonicSlicerCharged;
		destroyOnHit = true;
		vel.y = -500;
		if (type == 0) {dest = pos.addxy(-95, -92.5f);}
		if (type == 1) {dest = pos.addxy(-55, -100);}
		if (type == 2) {dest = pos.addxy(-0, -100);}
		if (type == 3) {dest = pos.addxy(55, -100);}
		if (type == 4) {dest = pos.addxy(95, -92.5f);}

		if (type == 0 || type == 4) {
			
		} else {

		}
		vel.x = 0;
		useGravity = false;

		if (rpc) {
			rpcCreate(pos, player, netProjId, 1);
		}
	}
		public override void update() {
		base.update();
		if (!fall) {
			if (Type == 0) {vel.y *= 0.932f;}
			if (Type == 1) {vel.y *= 0.95f;}
			if (Type == 2) {vel.y *= 0.95f;}
			if (Type == 3) {vel.y *= 0.95f;}
			if (Type == 4) {vel.y *= 0.934f;}
			float x = Helpers.lerp(pos.x, dest.x, -Global.spf * 2f);
			changePos(new Point(x, pos.y));
			// vel.y += 20;
		}
		if (pos.y <= dest.y) {
			fall = true;
			// vel.y = 0;
		}
		if (vel.y > 0) yDir = -1;
		if (fall) {
			if (vel.y < 300) {vel.y += 30;}
			else {vel.y = 300;}
		}
		
	}
	/* public override void update() {
		base.update();
		if (!fall) {
			if (pos.x != dest.x) {
				pos.x = (dest.x + pos.x) * Global.spf;
			} else {
				vel.x = 0;
			}
			changePos(new Point(pos.x, pos.y));
			vel.y += -20;
			if (pos.y <= dest.y) fall = true;
		}
		if (fall) {
			yDir = -1;
			vel.y += 20;
		}
		
	}*/
}