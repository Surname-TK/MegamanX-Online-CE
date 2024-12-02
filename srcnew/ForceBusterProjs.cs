using System;
using System.Collections.Generic;

namespace MMXOnline;

public class BusterStockProj : Projectile {
	public BusterStockProj(
		Weapon weapon, Point pos, int xDir, 
		Player player, ushort netProjId, bool rpc = false
	) : base(
		weapon, pos, xDir, 350, 2f, player, "buster_unpo", 
		0, 0, netProjId, player.ownedByLocalPlayer
	) {
		fadeSprite = "buster3_fade";
		fadeOnAutoDestroy = true;
		reflectable = true;
		maxTime = 0.5f;
		projId = (int)ProjIds.StockBuster;
		if (rpc) {
			rpcCreate(pos, player, netProjId, xDir);
		}
	}

	public static Projectile rpcInvoke(ProjParameters arg) {
		return new BusterStockProj(
			XBuster.netWeapon, arg.pos, arg.xDir, arg.player, arg.netId
		);
	}
}


public class BusterForcePlasmaProj : Projectile {
	public HashSet<IDamagable> hitDamagables = new HashSet<IDamagable>();

	public BusterForcePlasmaProj(
		Weapon weapon, Point pos, int xDir, 
		Player player, ushort netProjId, bool rpc = false
	) : base(
		weapon, pos, xDir, 360, 4f, player, "buster_plasma", 
		Global.defFlinch, 0.25f, netProjId, player.ownedByLocalPlayer
	) {
		fadeSprite = "buster4_x3_muzzle";
		fadeOnAutoDestroy = true;
		maxTime = 0.5f;
		projId = (int)ProjIds.PlasmaBuster;
		destroyOnHit = true;
		if (rpc) {
			rpcCreate(pos, player, netProjId, xDir);
		}
		xScale = 0.75f;
		yScale = 0.75f;
		releasePlasma = true;
	}

	public static Projectile rpcInvoke(ProjParameters arg) {
		return new BusterForcePlasmaProj(
			XBuster.netWeapon, arg.pos, arg.xDir, arg.player, arg.netId
		);
	}
}


public class BusterForcePlasmaHit : Projectile {
	public int type = 0;
	public float xDest = 0;
	public Actor actorOwner = null!;
	public Player player = null!;

	public BusterForcePlasmaHit(
		int type, Weapon weapon, Point pos, int xDir,
		Player player, ushort netProjId, bool rpc = false
	) : base(
		weapon, pos, xDir, 10, 0, player, "buster_plasma_hit", 
		Global.miniFlinch, 0.75f, netProjId, player.ownedByLocalPlayer
	) {
		zIndex -= 10;
		//fadeSprite = "buster_plasma_hit_exhaust";
		fadeOnAutoDestroy = true;
		maxTime = 2f;
		projId = (int)ProjIds.PlasmaBusterHit;
		destroyOnHit = false;
		shouldShieldBlock = false;
		shouldVortexSuck = false;
		// Hunter
		if (type == 1) {
			maxTime = 6;
			vel.x *= 1.5f;
		}
		// Various
		if (type == 2) {
			maxTime = 2.5f;
			vel.x *= 3;
		}
		// Slicer
		if (type == 3) {
			maxTime = 1;
			xDest = pos.x + (xDir * 30);
			vel.x = 0f;
			vel.y = -500f;
			useGravity = true;
			damager.flinch = Global.halfFlinch;
		}
		// Splasher
		if (type == 4) {
			maxTime = 4;
			vel.x = 0;
			vel.y = 0;
			if (player?.character != null) {
				actorOwner = player.character;
			}
			canBeLocal = false;
		}
		// Gravity Well, Lightning Web, Frost Tower
		if (type == 5 || type == 6) {
			maxTime = 4;
			vel.x = 0;
			vel.y = 0;
			canBeLocal = false;
		}
		// Double Cyclone
		if (type == 7) {
			vel.x *= 48;
			canBeLocal = false;
		}

		if (rpc) {
			byte[] extraArgs = new byte[] { (byte)type };

			rpcCreate(pos, player, netProjId, xDir, extraArgs);
		}

		this.type = type;
		this.player = player;
	}

	public override void update() {
		base.update();
		if (!ownedByLocalPlayer) {
			return;
		}
		// Slicer one.
		if (type == 3) {
			vel.y += Global.spf * Global.level.gravity;
			if (vel.y < 0) {
				float x = Helpers.lerp(pos.x, xDest, Global.spf * 10f);
				changePos(new Point(x, pos.y));
			}
		}
		// Splasher one.
		if (type == 4) {
			followOwner();
		}
		// Gravity/Web one.
		if (type == 5 || type == 6) {
			if (time < 1 && type == 5) {
				move(new Point(0, -60));
			} else {
				followTarget();
			}
		}
		//Double Cyclone one.
		if (type == 7) {
			if (Math.Abs(vel.x) > 30) vel.x -= xDir * Global.speedMul * 10; 
		}
	}

	public void followOwner() {
		if (actorOwner != null) {
			float targetPosX = (40 * -actorOwner.xDir + actorOwner.pos.x);
			float targetPosY = (-15 + actorOwner.pos.y + (2 - (Global.time % 2)));
			float moveSpeed = 1 * 60;

			// X axis follow.
			if (pos.x < targetPosX) {
				move(new Point(moveSpeed, 0));
				if (pos.x > targetPosX) { pos.x = targetPosX; }
			} else if (pos.x > targetPosX) {
				move(new Point(-moveSpeed, 0));
				if (pos.x < targetPosX) { pos.x = targetPosX; }
			}
			// Y axis follow.
			if (pos.y < targetPosY) {
				move(new Point(0, moveSpeed));
				if (pos.y > targetPosY) { pos.y = targetPosY; }
			} else if (pos.y > targetPosY) {
				move(new Point(0, -moveSpeed));
				if (pos.y < targetPosY) { pos.y = targetPosY; }
			}
		}
	}

	public void followTarget() {
		Actor? closestEnemy = Global.level.getClosestTarget(
			new Point (pos.x, pos.y),
			damager.owner.alliance,
			false, 200
		);

		if (closestEnemy == null) {
			return;
		}
		
		Point enemyPos = closestEnemy.getCenterPos();
		float moveSpeed = 1 * 60;

		// X axis follow.
		if (pos.x < enemyPos.x) {
			move(new Point(moveSpeed, 0));
			if (pos.x > enemyPos.x) { pos.x = enemyPos.x; }
		} else if (pos.x > enemyPos.x) {
			move(new Point(-moveSpeed, 0));
			if (pos.x < enemyPos.x) { pos.x = enemyPos.x; }
		}
		// Y axis follow.
		if (pos.y < enemyPos.y) {
			move(new Point(0, moveSpeed * 0.125f));
			if (pos.y > enemyPos.y) { pos.y = enemyPos.y; }
		} else if (pos.y > enemyPos.y) {
			move(new Point(0, -moveSpeed));
			if (pos.y < enemyPos.y) { pos.y = enemyPos.y; }
		}
	}

	public static Projectile rpcInvoke(ProjParameters arg) {
		return new BusterForcePlasmaHit(
			arg.extraData[0], XBuster.netWeapon, arg.pos, 
			arg.xDir, arg.player, arg.netId
		);
	}

	public override List<ShaderWrapper>? getShaders() {
		var shaders = new List<ShaderWrapper>();

		ShaderWrapper plasmaShader = Helpers.cloneShaderSafe("plasmaPalette");

		plasmaShader.SetUniform("palette", type);
		plasmaShader.SetUniform("paletteTexture", Global.textures["buster_plasma_hit_palette"]);
		shaders.Add(plasmaShader);
	
		if (shaders.Count > 0) {
			return shaders;
		} else {
			return base.getShaders();
		}
	}
}

public class ForceBuster3Proj : Projectile {

	public ForceBuster3Proj(
		Weapon weapon, Point pos, int xDir,
		 Player player, ushort netProjId, bool rpc = false
	) : base(
		weapon, pos, xDir, 240, 3f, player, "buster3_x4", 
		Global.halfFlinch, 0f, netProjId, player.ownedByLocalPlayer)
	{
		maxTime = 0.8f;
		fadeSprite = "buster2_fade";
		fadeOnAutoDestroy = true;
		projId = (int)ProjIds.PlasmaBuster3;

		if (rpc) rpcCreate(pos, player, netProjId, xDir);
	}

	public override void update() {
		base.update();
	
		vel.x += Global.spf * xDir * 550f;
		if (MathF.Abs(vel.x) > 300f) vel.x = 300 * xDir;
	}

	public static Projectile rpcInvoke(ProjParameters arg) {
		return new ForceBuster3Proj(
			XBuster.netWeapon, arg.pos, arg.xDir, arg.player, arg.netId
		);
	}
}
