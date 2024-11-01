using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace MMXOnline;
public class BusterProj : Projectile {
	public BusterProj(
		Weapon weapon, Point pos, int xDir, 
		int type, Player player, ushort netProjId, 
		bool rpc = false
	) : base
	(
		weapon, pos, xDir, 240, 1, 
		player, "buster1", 0, 0, netProjId, 
		player.ownedByLocalPlayer
	) {
		fadeSprite = "buster1_fade";
		reflectable = true;
		maxTime = 0.5175f;
		fadeOnAutoDestroy = true;
		if (type == 0) projId = (int)ProjIds.Buster;
		else if (type == 1) projId = (int)ProjIds.ZBuster;

		if (rpc) {
			byte[] extraArgs = new byte[] { (byte)type};
			rpcCreate(pos, player, netProjId, xDir, extraArgs);
		}
	}

	public static Projectile rpcInvoke(ProjParameters arg) {
		return new BusterProj(
			Buster.netWeapon, arg.pos, arg.xDir, 
			arg.extraData[0], arg.player, arg.netId
		);
	}

	public override void update() {
		base.update();
		if (System.MathF.Abs(vel.x) < 360) {
			vel.x += Global.spf * xDir * 900f;
			if (System.MathF.Abs(vel.x) >= 360) {
				vel.x = (float)xDir * 360;
			}
		}
	}
}

public class Buster2Proj : Projectile {
	public Buster2Proj(
		Weapon weapon, Point pos, int xDir, 
		Player player, ushort netProjId,
		bool rpc = false
	) : base(
		weapon, pos, xDir, 350, 2, 
		player, "buster2", 0, 0, netProjId, 
		player.ownedByLocalPlayer
	) {
		fadeSprite = "buster2_fade";
		reflectable = true;
		maxTime = 0.5f;
		projId = (int)ProjIds.Buster2;

		if (rpc) {
			rpcCreate(pos, player, netProjId, xDir);
		}
	}

	public static Projectile rpcInvoke(ProjParameters arg) {
		return new Buster2Proj(
			Buster.netWeapon, arg.pos, arg.xDir, 
			arg.player, arg.netId
		);
	}
}

public class BusterUnpoProj : Projectile {
	public BusterUnpoProj(
		Weapon weapon, Point pos, int xDir, 
		Player player, ushort netProjId,
		bool rpc = false	
	) : base(
		weapon, pos, xDir, 350, 3,
		 player, "buster_unpo", Global.defFlinch, 0.01f, 
		 netProjId, player.ownedByLocalPlayer
	) {
		fadeSprite = "buster3_fade";
		reflectable = true;
		maxTime = 0.5f;
		fadeOnAutoDestroy = true;
		projId = (int)ProjIds.BusterUnpo;

		if (rpc) {
			rpcCreate(pos, player, netProjId, xDir);
		}
	}

	public static Projectile rpcInvoke(ProjParameters arg) {
		return new BusterUnpoProj(
			Buster.netWeapon, arg.pos, arg.xDir, 
			arg.player, arg.netId
		);
	}
}

public class Buster3Proj : Projectile {
	public MegamanX mmx;
	public int type;
	public List<Sprite> spriteMids = new List<Sprite>();
	float partTime;

	public Buster3Proj(
		Weapon weapon, Point pos, int xDir, 
		int type, Player player, ushort netProjId,
		bool rpc = false
	) : base(
		weapon, pos, xDir, 350, 3, 
		player, "buster3", Global.defFlinch, 0f, 
		netProjId, player.ownedByLocalPlayer
	) {
		this.type = type;
		mmx = player.character as MegamanX;
		maxTime = 0.5f;
		fadeOnAutoDestroy = true;
		fadeSprite = "buster3_fade";
		projId = (int)ProjIds.Buster3;
		reflectable = true;
		int Charge = player.character.getChargeLevel();

		if (rpc) {
			byte[] extraArgs = new byte[] { (byte)type};
			rpcCreate(pos, player, netProjId, xDir, extraArgs);
		}

		// Big fat mess lol
		if (player.hasArmArmor(ArmorId.Giga)) {
			changeSprite("buster3_x2", true);
			if (Charge > 2){
					projId = (int)ProjIds.Buster4;
					damager.flinch = Global.defFlinch;
					reflectable = false;
			} else if (mmx.stockedX2Charge == true) {
				changeSprite("buster4_x2", true);
				fadeSprite = "buster4_x2_fade";
				projId = (int)ProjIds.Buster4;
				damager.flinch = Global.defFlinch;
				reflectable = false;
				for (int i = 0; i < 6; i++) {
					var midSprite = new Sprite("buster4_x2_orbit");
					spriteMids.Add(midSprite);
				}
			} else {
				damager.flinch = Global.halfFlinch;
			}
		} else if (player.hasArmArmor(ArmorId.Max)) {
			damager.flinch = Global.halfFlinch;
			changeSprite("buster3_x3", true);
		}

		// Cross Shot Finale
		if (type == 1) {
			damager.damage = 4;
			changeSprite("buster4_x3", true);
			fadeSprite = "buster4_x2_fade";
			vel.x = 0;
			maxTime = 1.5f;
			projId = (int)ProjIds.Buster4;
			reflectable = false;
		}
		fadeOnAutoDestroy = true;
	}
	// Down here is where the Cross Shot actually happens
	public override void onCollision(CollideData other) {
		base.onCollision(other);	
			if (other.gameObject is BusterX3Proj1 X3shot && X3shot.ownedByLocalPlayer && !destroyed) {
				if (!ownedByLocalPlayer) return;
					Global.level.delayedActions.Add(new DelayedAction(delegate {
						new Anim(new Point(pos.x, pos.y), "buster4_x3_muzzle", xDir, null, true, true);
						destroySelfNoEffect(); X3shot.destroySelfNoEffect();
						Global.level.delayedActions.Add(new DelayedAction(delegate { 
						if (!owner.hasUltimateArmor()) {
							new Buster3Proj(
								weapon, pos, xDir, 1, owner, owner.getNextActorNetId(), rpc: true
							);
						} else {
							new BusterPlasmaProj(
								weapon, pos, xDir, owner, owner.getNextActorNetId(), rpc: true
							);
							playSound("plasmaShot", sendRpc: true);
						}
						new BusterX3Proj3(
							weapon, pos, xDir, 0, owner, owner.getNextActorNetId(), rpc: true
						);
						new BusterX3Proj3(
							weapon, pos, xDir, 1, owner, owner.getNextActorNetId(), rpc: true
						);
						new BusterX3Proj3(
							weapon, pos, xDir, 2, owner, owner.getNextActorNetId(), rpc: true
						);
						new BusterX3Proj3(
							weapon, pos, xDir, 3, owner, owner.getNextActorNetId(), rpc: true
					);
					}, 20f / 60f ));
					}, 1f / 60f ));
			}
	}

	public static Projectile rpcInvoke(ProjParameters arg) {
		return new Buster3Proj(
			Buster.netWeapon, arg.pos, arg.xDir, 
			arg.extraData[0], arg.player, arg.netId
		);
	}

	public override void update() {
		base.update();
		/*if (sprite.name is "buster4_x2") {
			vel.x += Global.spf * xDir * 550;
			if (MathF.Abs(vel.x) > 300) vel.x = 300 * xDir;
			partTime += Global.spf;
			if (partTime > 0.05f) {
				partTime = 0;
				new Anim(pos.addRand(0, 16), "buster4_x3_part", 1, null, true) { acc = new Point(-vel.x * 3f, 0) };
			}
		} else */if (sprite.name is "buster3_x3") {
			vel.x += Global.spf * xDir * 550;
			if (MathF.Abs(vel.x) > 350) vel.x = 350 * xDir;
			partTime += Global.spf;
			if (partTime > 0.075f) {
			partTime = 0;
			new Anim(pos.addxy(20 * xDir, 0).addRand(0, 16), "buster4_x3_part", 1, null, true) {acc = new Point(-vel.x / 2, 0) };
			}
		} else if (sprite.name is "buster4_x3") {
			vel.x += Global.spf * xDir * 450;
			if (MathF.Abs(vel.x) > 350) { vel.x = 350 * xDir; }
			partTime += Global.spf;
			if (partTime > 0.05f) {
				partTime = 0;
				new Anim(pos.addRand(0, 16), "buster4_x3_part", 1, null, true) { acc = new Point((MathF.Abs(vel.x) + 50) * 3f * -xDir, 0) };
			}
		}
	}

	public override void render(float x, float y) {
		base.render(x, y);
		if (sprite.name is "buster4_x2") {
			float piHalf = MathF.PI / 2;
			float xOffset = 8;
			float partTime = (time * 0.75f);
			for (int i = 0; i < 6; i++) {
				float t = 0;
				float xOff2 = 0;
				float sinX = 0;
				if (i < 3) {
					t = partTime - (i * 0.025f);
					xOff2 = i * xDir * 3;
					sinX = 5 * MathF.Cos(partTime * 20);
				} else {
					t = partTime + (MathF.PI / 4) - ((i - 3) * 0.025f);
					xOff2 = (i - 3) * xDir * 3;
					sinX = 5 * MathF.Sin((partTime) * 20);
				}
				float sinY = 15 * MathF.Sin(t * 20);
				long zIndexTarget = zIndex - 1;
				float currentOffset = (t * 20) % (MathF.PI * 2);
				if (currentOffset > piHalf && currentOffset < piHalf * 3) {
					zIndexTarget = zIndex + 1;
				}
				spriteMids[i].draw(
					spriteMids[i].frameIndex,
					pos.x + x + sinX - xOff2 + xOffset,
					pos.y + y + sinY, xDir, yDir,
					getRenderEffectSet(), 1, 1, 1, zIndexTarget
				);
				spriteMids[i].update();
			}
		}
	}
}

public class Buster4Proj : Projectile {
	public int type = 0;
	public float offsetTime = 0;
	public float initY = 0;
	bool smoothStart = false;

	public Buster4Proj(
		Weapon weapon, Point pos, int xDir, Player player,
		int type, float offsetTime, ushort netProjId,
		bool smoothStart = false, bool rpc = false
	) : base(
		weapon, pos, xDir, 396, 3, player, "buster4",
		Global.defFlinch, 1f, netProjId, player.ownedByLocalPlayer
	) {
		fadeSprite = "buster4_fade";
		this.type = type;
		initY = this.pos.y;
		this.offsetTime = offsetTime;
		this.smoothStart = smoothStart;
		maxTime = 0.6f;
		fadeOnAutoDestroy = true;
		projId = (int)ProjIds.Buster4;

		if (rpc) {
			byte[] extraArgs = new byte[] { (byte)type, (byte)offsetTime};
			rpcCreate(pos, player, netProjId, xDir, extraArgs);
		}
	}

	public static Projectile rpcInvoke(ProjParameters arg) {
		return new Buster4Proj(
			Buster.netWeapon, arg.pos, arg.xDir, arg.player,
			arg.extraData[0], arg.extraData[1], arg.netId
		);
	}

	public override void update() {
		base.update();
		base.frameIndex = type;
		float currentOffsetTime = offsetTime;
		if (smoothStart && time < 5f / 60f) {
			currentOffsetTime *= (time / 5f) * 60f;
		}
		float y = initY + (MathF.Sin((time + currentOffsetTime) * (MathF.PI * 6)) * 15f);
		changePos(new Point(pos.x, y));
	}
}

public class BusterX3Proj1 : Projectile {
	public int type;
	public List<Sprite> spriteMids = new List<Sprite>();
	float offsetTime = 0;
	float initY = 0;
	float line1Y = 0;
	float line2Y = -2;
	float line3Y = 2;
	float partTime;
		public BusterX3Proj1(Weapon weapon, Point pos, int xDir, int type, Player player, ushort netProjId, bool rpc = false) :
		base(weapon, pos, xDir, 350, 1, player, "buster4_max_orb2", Global.halfFlinch, 0f, netProjId, player.ownedByLocalPlayer) {
		this.type = type;
		maxDistance = 175;
		vel.x = 0;
		fadeSprite = "buster3_fade";
		fadeOnAutoDestroy = true;
		projId = (int)ProjIds.BusterX3Proj1;
		reflectable = false;
		
		if (rpc) {
			rpcCreate(pos, player, netProjId, xDir, (byte)type);
		}

	}
	public override void update() {
		base.update();
		vel.x += Global.spf * xDir * 300;
		if (MathF.Abs(vel.x) > 300) { vel.x = 300 * xDir; }
		frameIndex = type;
		float currentOffsetTime = offsetTime;
		if (time < 5f / 60f) {
			currentOffsetTime *= time / 5f * 60f;
		}
		float zLayer = 10;
		line1Y = initY + (MathF.Sin((time + currentOffsetTime) * (MathF.PI * 6)) * 15f);
		line2Y = initY + (MathF.Sin((time + currentOffsetTime) * (MathF.PI * 6)) * 15f);
		line3Y = initY + (MathF.Sin((time + currentOffsetTime) * (MathF.PI * 6)) * 15f);
		new Anim(new Point(pos.x - 4, pos.y + line1Y), "buster4_max_orb1", 1, null, true, zIndex == zLayer + line1Y);
		new Anim(new Point(pos.x, pos.y - 4 + (line2Y * -1)), "buster4_max_orb2", xDir, null, true, zIndex == zLayer + line2Y);
		new Anim(new Point(pos.x + 4, pos.y + line3Y), "buster4_max_orb3", xDir, null, true, zIndex == zLayer + line3Y);
	}
	/*
	public override void onCollision(CollideData other) {
		base.onCollision(other);
		if(other.gameObject is Buster3Proj X3shot && X3shot.ownedByLocalPlayer && !destroyed){
			Global.level.delayedActions.Add(new DelayedAction(delegate { 
			// fadeSprite = null;
			// is there any better code to use no fadeSprite?
			destroySelf(); 
			// X3shot.destroySelf();
			}, 1f / 60f ));
		}
	}
	*/

	// This down here is meant for the split shot when hitting another character
	public override void onHitDamagable(IDamagable damagable) {
		base.onHitDamagable(damagable);
		if (ownedByLocalPlayer) {
				fadeSprite = "buster3_fade";
				destroySelf();
				Global.level.delayedActions.Add(new DelayedAction(delegate { 
					new BusterX3Proj2(weapon, pos, xDir, 0, owner, owner.getNextActorNetId(), rpc: true);
					new BusterX3Proj2(weapon, pos, xDir, 1, owner, owner.getNextActorNetId(), rpc: true);
				}, 2f / 60f ));
		}
	}
}

public class BusterX3Proj2 : Projectile {
	public int type = 0;
	public List<Point> lastPositions = new List<Point>();
	public BusterX3Proj2(
		Weapon weapon, Point pos, int xDir, int type,
		Player player, ushort netProjId, bool rpc = false
	) : base(
		weapon, pos, xDir, 400, 1,
		player, type == 0 || type == 1 ? "buster4_max_orb1" : "buster4_max_orb3",
		0, 0, netProjId, player.ownedByLocalPlayer
	) {
		fadeSprite = "buster4_fade";
		this.type = type;
		reflectable = true;
		maxTime = 1f;
		fadeOnAutoDestroy = true;
		projId = (int)ProjIds.BusterX3Proj2;
		if (type == 0) { changeSprite("buster4_max_orb3", true); vel = new Point(-250 * xDir, -75);}
		if (type == 1) { changeSprite("buster4_max_orb1", true); vel = new Point(-250 * xDir, 75);}
		frameSpeed = 0;
		frameIndex = 0;

		if (rpc) {
			rpcCreate(pos, player, netProjId, xDir, (byte)type);
		}
	}

	public override void update() {
		base.update();
		float maxSpeed = 300;
		vel.inc(new Point(Global.spf * 750 * xDir, 0));
		if (MathF.Abs(vel.x) > maxSpeed) vel.x = maxSpeed * xDir;
		lastPositions.Add(pos);
		if (lastPositions.Count > 4) lastPositions.RemoveAt(0);
	}

	public override void render(float x, float y) {
		string spriteName = type == 0 || type == 1 ? "buster4_max_orb3" : "buster4_max_orb1";
		//if (lastPositions.Count > 3) Global.sprites[spriteName].draw(1, lastPositions[3].x + x, lastPositions[3].y + y, 1, 1, null, 1, 1, 1, zIndex);
		if (lastPositions.Count > 2) Global.sprites[spriteName].draw(2, lastPositions[2].x + x, lastPositions[2].y + y, 1, 1, null, 1, 1, 1, zIndex);
		if (lastPositions.Count > 1) Global.sprites[spriteName].draw(3, lastPositions[1].x + x, lastPositions[1].y + y, 1, 1, null, 1, 1, 1, zIndex);
		base.render(x, y);
	}
}
public class BusterX3Proj3 : Projectile {
	public int type = 0;
	public List<Point> lastPositions = new List<Point>();
	public BusterX3Proj3(
		Weapon weapon, Point pos, int xDir, int type,
		Player player, ushort netProjId, bool rpc = false
	) : base(
		weapon, pos, xDir, 400, 1,
		player, type == 0 || type == 3 ? "buster4_x3_orbit" : "buster4_x3_orbit2",
		0, 0, netProjId, player.ownedByLocalPlayer
	) {
		fadeSprite = "buster4_fade";
		this.type = type;
		reflectable = true;
		maxTime = 1f;
		fadeOnAutoDestroy = true;
		projId = (int)ProjIds.BusterX3Proj3;
		if (type == 0) vel = new Point(-450 * xDir, -75);
		if (type == 1) vel = new Point(-400 * xDir, -50);
		if (type == 2) vel = new Point(-400 * xDir, 50);
		if (type == 3) vel = new Point(-450 * xDir, 75);
		frameSpeed = 0;
		frameIndex = 0;

		if (rpc) {
			rpcCreate(pos, player, netProjId, xDir, (byte)type);
		}
	}

	public static Projectile rpcInvoke(ProjParameters arg) {
		return new BusterX3Proj2(
			Buster.netWeapon, arg.pos, arg.xDir, 
			arg.extraData[0], arg.player, arg.netId
		);
	}

	public override void update() {
		base.update();
		float maxSpeed = 600;
		vel.inc(new Point(Global.spf * 1500 * xDir, 0));
		if (MathF.Abs(vel.x) > maxSpeed) vel.x = maxSpeed * xDir;
		lastPositions.Add(pos);
		if (lastPositions.Count > 4) lastPositions.RemoveAt(0);
	}

	public override void render(float x, float y) {
		string spriteName = type == 0 || type == 3 ? "buster4_x3_orbit" : "buster4_x3_orbit2";
		//if (lastPositions.Count > 3) Global.sprites[spriteName].draw(1, lastPositions[3].x + x, lastPositions[3].y + y, 1, 1, null, 1, 1, 1, zIndex);
		if (lastPositions.Count > 2) Global.sprites[spriteName].draw(2, lastPositions[2].x + x, lastPositions[2].y + y, 1, 1, null, 1, 1, 1, zIndex);
		if (lastPositions.Count > 1) Global.sprites[spriteName].draw(3, lastPositions[1].x + x, lastPositions[1].y + y, 1, 1, null, 1, 1, 1, zIndex);
		base.render(x, y);
	}
}

public class BusterPlasmaProj : Projectile {
	public HashSet<IDamagable> hitDamagables = new HashSet<IDamagable>();
	public BusterPlasmaProj(
		Weapon weapon, Point pos, int xDir, 
		Player player, ushort netProjId, bool rpc = false
	) : base(
		weapon, pos, xDir, 400, 4, player, "buster_plasma",
		Global.defFlinch, 0.25f, netProjId, player.ownedByLocalPlayer
	) {
		maxTime = 0.5f;
		projId = (int)ProjIds.BusterX3Plasma;
		destroyOnHit = false;
		xScale = 0.75f;
		yScale = 0.75f;

		if (rpc) {
			rpcCreate(pos, player, netProjId, xDir);
		}
	}

	public static Projectile rpcInvoke(ProjParameters arg) {
		return new BusterPlasmaProj(
			Buster.netWeapon, arg.pos, arg.xDir, 
			arg.player, arg.netId
		);
	}

	public override void onHitDamagable(IDamagable damagable) {
		base.onHitDamagable(damagable);
		if (ownedByLocalPlayer && hitDamagables.Count < 1) {
			if (!hitDamagables.Contains(damagable)) {
				hitDamagables.Add(damagable);
				float xThreshold = 10;
				Point targetPos = damagable.actor().getCenterPos();
				float distToTarget = MathF.Abs(pos.x - targetPos.x);
				Point spawnPoint = pos;
				if (distToTarget > xThreshold) spawnPoint = new Point(targetPos.x + xThreshold * Math.Sign(pos.x - targetPos.x), pos.y);
				new BusterPlasmaHitProj(weapon, spawnPoint, xDir, owner, owner.getNextActorNetId(), rpc: true);
			}
		}
	}
}

public class BusterPlasmaHitProj : Projectile {
	public BusterPlasmaHitProj(
		Weapon weapon, Point pos, int xDir, 
		Player player, ushort netProjId, bool rpc = false
	) : base(
		weapon, pos, xDir, 0, 1, player, "buster_plasma_hit", 
		0, 0.25f, netProjId, player.ownedByLocalPlayer
	) {
		maxTime = 2f;
		projId = (int)ProjIds.BusterX3PlasmaHit;
		destroyOnHit = false;
		netcodeOverride = NetcodeModel.FavorDefender;

		if (rpc) {
			rpcCreate(pos, player, netProjId, xDir);
		}
	}

	public static Projectile rpcInvoke(ProjParameters arg) {
		return new BusterPlasmaHitProj(
			Buster.netWeapon, arg.pos, arg.xDir, 
			arg.player, arg.netId
		);
	}
}
