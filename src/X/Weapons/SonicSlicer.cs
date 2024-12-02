using System;
using System.Collections.Generic;
using System.Globalization;

namespace MMXOnline;

public class SonicSlicer : Weapon {
	public static SonicSlicer netWeapon = new();

	public SonicSlicer() : base() {
		shootSounds = new string[] { "sonicSlicer", "sonicSlicer", "sonicSlicer", "sonicSlicerCharged" };
		fireRate = 30;
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

	public override void shoot(Character character, int[] args) {
		int chargeLevel = args[0];
		Point pos = character.getShootPos();
		int xDir = character.getShootXDir();
		Player player = character.player;

		if (chargeLevel < 3) {
			new SonicSlicerStart(this, pos, xDir, player, player.getNextActorNetId(), true);
		} else {
			new Anim(pos, "sonicslicer_charge_start", xDir, null, true);
			player.setNextActorNetId(player.getNextActorNetId());
			new SonicSlicerProjCharged(this, pos, 0, player, player.getNextActorNetId(true), true);
			new SonicSlicerProjCharged(this, pos, 1, player, player.getNextActorNetId(true), true);
			new SonicSlicerProjCharged(this, pos, 2, player, player.getNextActorNetId(true), true);
			new SonicSlicerProjCharged(this, pos, 3, player, player.getNextActorNetId(true), true);
			new SonicSlicerProjCharged(this, pos, 4, player, player.getNextActorNetId(true), true);

			if (player.hasPlasma() && player.ownedByLocalPlayer) {
				new BusterForcePlasmaHit(3, this, pos, xDir, player, player.getNextActorNetId(), rpc: true);
			}
		}
	}
}

public class SonicSlicerStart : Projectile {
	public SonicSlicerStart(
		Weapon weapon, Point pos, int xDir, 
		Player player, ushort netProjId, bool rpc = false
	) : base(
		weapon, pos, xDir, 0, 1, player, "sonicslicer_start", 
		0, 0, netProjId, player.ownedByLocalPlayer
	) {
		projId = (int)ProjIds.SonicSlicerStart;

		if (rpc) {
			rpcCreate(pos, player, netProjId, xDir);
		}
	}

	public static Projectile rpcInvoke(ProjParameters arg) {
		return new SonicSlicerStart(
			SonicSlicer.netWeapon, arg.pos, arg.xDir, arg.player, arg.netId
		);
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
	public float BounceTime = 0;
	int type;
	public SonicSlicerProj(
		Weapon weapon, Point pos, int xDir, int type, 
		Player player, ushort netProjId, bool rpc = false
	) : base(
		weapon, pos, xDir, 0, 1, player, "sonicslicer_proj", 
		0, 0, netProjId, player.ownedByLocalPlayer
	) {
		maxTime = 2f;
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

	public static Projectile rpcInvoke(ProjParameters arg) {
		return new SonicSlicerProj(
			SonicSlicer.netWeapon, arg.pos, arg.xDir, 
			arg.extraData[0], arg.player, arg.netId
		);
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
				if (BounceTime > 1f) {
					destroySelfNoEffect();
				}
				playSound("dingX2");
				BounceTime += 0.2f;
				xDir *= -1;
				time -= 0.25f;
				vel.x *= -1;
				new Anim(pos, "sonicslicer_sparks", xDir, null, true);
				//RPC.actorToggle.sendRpc(netId, RPCActorToggleType.SonicSlicerBounce);
			} else {
				if (BounceTime >= 0.2f) {
					BounceTime -= 0.2f;
				}
			}

			int velYSign = MathF.Sign(vel.y);
			if (velYSign != 0) {
				collideData = Global.level.checkTerrainCollisionOnce(this, 0, velYSign, vel);
				if (collideData != null && collideData.hitData != null) {
					if (BounceTime > 1f) {
						destroySelfNoEffect();
					}
					playSound("dingX2");
					BounceTime += 0.2f;
					vel.y *= -1;
					time -= 0.25f;
					Curve *= -1;
					new Anim(pos, "sonicslicer_sparks", xDir, null, true);
					//RPC.actorToggle.sendRpc(netId, RPCActorToggleType.SonicSlicerBounce);
			} else {
				if (BounceTime >= 0.2f) {
					BounceTime -= 0.2f;
				}
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
	public SonicSlicerProjCharged(
		Weapon weapon, Point pos, int type, 
		Player player, ushort netProjId, bool rpc = false
	) : base(
		weapon, pos, 1, 300, 2, player, "sonicslicer_charged", 
		Global.defFlinch, 0.02f, netProjId, player.ownedByLocalPlayer
	) {
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
			rpcCreate(pos, player, netProjId, 1, (byte)type);
		}
	}

	public static Projectile rpcInvoke(ProjParameters arg) {
		return new SonicSlicerProjCharged(
			SonicSlicer.netWeapon, arg.pos, arg.extraData[0], arg.player, arg.netId
		);
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
