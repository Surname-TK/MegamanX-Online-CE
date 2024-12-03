using System;
using System.Collections.Generic;

namespace MMXOnline;

public class GroundHunter : Weapon {

	public static GroundHunter netWeapon = new();

	public GroundHunter() : base() {
		index = (int)WeaponIds.GroundHunter;
		fireRate = 30;
		weaponSlotIndex = 127;
		weaponBarIndex = 65;
		weaponBarBaseIndex = 76;
		shootSounds = new string[] {"","","",""};
		weaknessIndex = (int)WeaponIds.FrostTower;
		/* damage = "2/1-1";
		hitcooldown = "0/0.5-0";
		Flinch = "0";
		FlinchCD = hitcooldown;
		effect = "Press DOWN to change direction. C: Press UP or DOWN to spawn more projectiles."; */
	}

	public override void shoot(Character character, int[] args) {
		int chargeLevel = args[0];
		Point pos = character.getShootPos();
		int xDir = character.getShootXDir();
		Player player = character.player;

		if (chargeLevel >= 3) {
			new GroundHunterChargedProj(this, pos, xDir, player, player.getNextActorNetId(), true);
		} else {
			new GroundHunterProj(this, pos, xDir, player, player.getNextActorNetId(), true);
		}
	}
}


public class GroundHunterProj : Projectile {

	const float projSpeed = 240;
	bool downPressed;
	bool down;
	Player player;
	bool groundedOnce;
	Anim? sparks;

	public GroundHunterProj(
		Weapon weapon, Point pos, int xDir,
		Player player, ushort? netProjId,
		bool rpc = false
	) : base(
		weapon, pos, xDir, projSpeed, 2,
		player, "ground_hunter_proj", 0, 0,
		netProjId, player.ownedByLocalPlayer
	) {
		projId = (int)ProjIds.GroundHunter;
		this.player = player;
		wallCrawlSpeed = projSpeed;
		maxTime = 0.75f;
		useGravity = true;
		gravityModifier = 0.5f;
		fadeSprite = "ground_hunter_fade";
		canBeLocal = false;

		if (rpc) rpcCreate(pos, player, netProjId, xDir);
	}

	public static Projectile rpcInvoke(ProjParameters arg) {
		return new GroundHunterProj(
			GroundHunter.netWeapon, arg.pos, arg.xDir, arg.player, arg.netId
		);
	}

	public override void update() {
		base.update();

		//gravityModifier = Helpers.lerp(gravityModifier, 1, (Global.speedMul * 1.5f) / 60);
		
		updateWallCrawl();
		if (sparks != null) {
			sparks.changePos(pos);
		}

		downPressed = player.input.isPressed(Control.Down, player);

		if (downPressed && !down && !groundedOnce) {
			down = true;
			changeSprite("ground_hunter_fall", false);
			stopMoving();
			vel.y = Physics.MaxFallSpeed;
		}
		
		if (deltaPos.y > 0 && !down && groundedOnce) {
			down = true;
			changeSprite("ground_hunter_fall", false);
			if (sparks != null) {
				sparks.destroySelf();
				sparks = null!;
			}
		}
		else if (deltaPos.y < 0) destroySelf();
		else if (down && deltaPos.y == 0 && !downPressed) {
			down = false;
			changeSprite("ground_hunter_proj", false);
			
			if (sparks == null) {
				sparks = new Anim(pos, "ground_hunter_sparks", xDir,
				damager.owner.getNextActorNetId(), true, true);
			}
		} 
	}

	public override void onHitWall(CollideData other) {
		base.onHitWall(other);

		vel.x = 0;
		vel.y = 0;
		useGravity = false;
		setupWallCrawl(new Point(xDir, 1));
		updateWallCrawl();
		groundedOnce = true;

		if (other.isSideWallHit() && !down) destroySelf();
	}

	public override void onDestroy() {
		base.onDestroy();
		sparks?.destroySelf();
	}
}


public class GroundHunterChargedProj : Projectile {

	bool fired;
	Player player;
	int shootCount;
	int shootCooldown;

	public GroundHunterChargedProj(
		Weapon weapon, Point pos, int xDir,
		Player player, ushort? netProjId,
		bool rpc = false
	) : base(
		weapon, pos, xDir, 300, 2,
		player, "ground_hunter_charged_proj", 0, 0.5f,
		netProjId, player.ownedByLocalPlayer
	) {
		projId = (int)ProjIds.GroundHunterCharged;
		maxTime = 1f;
		this.player = player;
		destroyOnHit = false;
		releasePlasma = player.hasPlasma();
		canBeLocal = false;

		if (rpc) rpcCreate(pos, player, netProjId, xDir);
	}

	public static Projectile rpcInvoke(ProjParameters arg) {
		return new GroundHunterChargedProj(
			GroundHunter.netWeapon, arg.pos, arg.xDir, arg.player, arg.netId
		);
	}

	public override void update() {
		base.update();

		if (shootCooldown > 0) shootCooldown--;

		if (player.input.getYDir(player) != 0 && !fired) {
			fired = true;
			time = 0;
		}

		else if (fired && shootCooldown <= 0 && shootCount < 5) {
			shootCooldown = 4;
			shootCount++;
			
			new GroundHunterSmallProj(weapon, pos, xDir, player, 1, player.getNextActorNetId(), true);
			new GroundHunterSmallProj(weapon, pos, xDir, player, 2, player.getNextActorNetId(), true);
		}

		else if (shootCount >= 5) destroySelf();
	}
}


public class GroundHunterSmallProj : Projectile {
	
	public GroundHunterSmallProj(
		Weapon weapon, Point pos, int xDir,
		Player player, int type, ushort? netProjId,
		bool rpc = false
	) : base(
		weapon, pos, xDir, 0, 1,
		player, "ground_hunter_small_proj", 0, 0,
		netProjId, player.ownedByLocalPlayer
	) {
		projId = (int)ProjIds.GroundHunterSmall;
		maxTime = 0.33f;
		if (type == 2) yScale *= -1;
		vel.y = -yScale * 300;

		if (rpc) {
			byte[] extraArgs = new byte[] { (byte)type };

			rpcCreate(pos, player, netProjId, xDir, extraArgs);
		}
	}

	public static Projectile rpcInvoke(ProjParameters arg) {
		return new GroundHunterSmallProj(
			GroundHunter.netWeapon, arg.pos, arg.xDir, 
			arg.player, arg.extraData[0], arg.netId
		);
	}
}
