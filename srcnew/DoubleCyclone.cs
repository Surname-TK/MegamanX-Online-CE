using System;
using System.Collections.Generic;

namespace MMXOnline;

public class DoubleCyclone : Weapon {

	public static DoubleCyclone netWeapon = new();

	public DoubleCyclone() : base() {
		index = (int)WeaponIds.DoubleCyclone;
		fireRate = 60;
		weaponSlotIndex = 129;
		weaponBarBaseIndex = 78;
        weaponBarIndex = 67;
		shootSounds = new string[] {"","","",""};
		weaknessIndex = (int)WeaponIds.AimingLaser;
		hasCustomAnim = true;
		/* damage = "1";
		hitcooldown = "0.33";
		Flinch = "0";
		FlinchCD = hitcooldown;
		effect = "Pushes enemies away."; */
	}

	public override void shoot(Character character, int[] args) {
		int chargeLevel = args[0];

		character.changeState(new DoubleCycloneState(chargeLevel), true);
	}
}


public class DoubleCycloneState : CharState {

	bool fired;
	bool condition;
	float chargeLv;
	MegamanX mmx = null!;

	public DoubleCycloneState(float chargeLv) : base("double_cyclone") {
		normalCtrl = false;
		attackCtrl = false;
		this.chargeLv = chargeLv;
	}

	public override void onEnter(CharState oldState) {
		base.onEnter(oldState);
		mmx = character as MegamanX ?? throw new NullReferenceException();
		character.useGravity = false;
		character.stopMoving();
	}

	public override void update() {
		base.update();
		if (chargeLv >= 3) {
			if (!fired && character.currentFrame.getBusterOffset() != null) {
				Point? shootPos1 = character.getFirstPOI() ?? character.getShootPos();
				Point? shootPos2 = character.getFirstPOI(1) ?? character.getShootPos();
				int xDir = character.getShootXDir();
				Player player = character.player;

				new DoubleCycloneChargedSpawn(new DoubleCyclone(), shootPos1.Value, -xDir,
					player, player.getNextActorNetId(), true);
				new DoubleCycloneChargedSpawn(new DoubleCyclone(), shootPos2.Value, xDir,
					player, player.getNextActorNetId(), true);
				
				fired = true;

				if (player.hasPlasma()) {
					new BusterForcePlasmaHit(7, player.weapon, shootPos1.Value, -xDir,
						player, player.getNextActorNetId(), true);
					new BusterForcePlasmaHit(7, player.weapon, shootPos2.Value, xDir,
						player, player.getNextActorNetId(), true);
				}
			}

			if (mmx.dCycloneSpawn == null && mmx.isAnimOver()) mmx.changeToIdleOrFall();
		}

		else {
			if (!fired && character.currentFrame.getBusterOffset() != null) {
				Point? shootPos1 = character.getFirstPOI() ?? character.getShootPos();
				Point? shootPos2 = character.getFirstPOI(1) ?? character.getShootPos();
				int xDir = character.getShootXDir();
				Player player = character.player;
			
				new DoubleCycloneProj(new DoubleCyclone(), shootPos1.Value, -xDir, player, player.getNextActorNetId(), true);
				new DoubleCycloneProj(new DoubleCyclone(), shootPos2.Value, xDir, player, player.getNextActorNetId(), true);
				fired = true;
				condition = true;
			}

			if (character.isAnimOver() && condition) character.changeToIdleOrFall();if (!fired && character.currentFrame.getBusterOffset() != null) {
				Point? shootPos1 = character.getFirstPOI() ?? character.getShootPos();
				Point? shootPos2 = character.getFirstPOI(1) ?? character.getShootPos();
				int xDir = character.getShootXDir();
				Player player = character.player;
			
				new DoubleCycloneProj(new DoubleCyclone(), shootPos1.Value, -xDir, player, player.getNextActorNetId(), true);
				new DoubleCycloneProj(new DoubleCyclone(), shootPos2.Value, xDir, player, player.getNextActorNetId(), true);
				fired = true;
				condition = true;
			}	

			if (character.isAnimOver() && condition) character.changeToIdleOrFall();
		}
	}

	public override void onExit(CharState newState) {
		base.onExit(newState);
		character.useGravity = true;
	}
}


public class DoubleCycloneProj : Projectile {

	int screenFrames;

	public DoubleCycloneProj(
		Weapon weapon, Point pos, int xDir,
		Player player, ushort? netProjId,
		bool rpc = false
	) : base (
		weapon, pos, xDir, 0, 1,
		player, "double_cyclone_proj", 0, 0.33f,
		netProjId, player.ownedByLocalPlayer
	) {
		projId = (int)ProjIds.DoubleCyclone;
		maxTime = 1f;
		destroyOnHit = false;
		shouldShieldBlock = false;

		if (rpc) rpcCreate(pos, player, netProjId, xDir);
	}

	public static Projectile rpcInvoke(ProjParameters arg) {
		return new DoubleCycloneProj(
			DoubleCyclone.netWeapon, arg.pos, arg.xDir, arg.player, arg.netId
		);
	}

	public override void update() {
		base.update();

		if (vel.y > -120 && screenFrames >= 15) {
			vel.y -= Global.speedMul * 6;
			vel.x -= Global.speedMul * xDir * 6;
		} 
		if (Math.Abs(vel.x) < 120 && screenFrames < 15) vel.x += xDir * Global.speedMul * 4;
		screenFrames++;
	}

	public override void onHitDamagable(IDamagable damagable) {
		base.onHitDamagable(damagable);
		var chr = damagable as Character;
		var mav = damagable as Maverick;

		if (chr != null) {
			chr.xPushVel = vel.x;
			chr.yPushVel = vel.y * 2;
		}

		if (mav != null) {
			mav.xPushVel = vel.x * 0.5f;
			mav.yPushVel = vel.y;
		}	
	}
}


public class DoubleCycloneChargedSpawn : Projectile {

	int shootCooldown;
	int shootCount;
	MegamanX mmx = null!;

	public DoubleCycloneChargedSpawn(
		Weapon weapon, Point pos, int xDir,
		Player player, ushort? netProjId,
		bool rpc = false
	) : base(
		weapon, pos, xDir, 0, 1,
		player, "double_cyclone_charged_spawn", 0, 0.33f,
		netProjId, player.ownedByLocalPlayer
	) {
		projId = (int)ProjIds.DoubleCycloneChargedSpawn;
		maxTime = 2f;
		destroyOnHit = false;
		mmx = player.character as MegamanX ?? throw new NullReferenceException();
		mmx.dCycloneSpawn = this;
		shouldShieldBlock = false;

		if (rpc) rpcCreate(pos, player, netProjId, xDir);
	}

	public static Projectile rpcInvoke(ProjParameters arg) {
		return new DoubleCycloneChargedSpawn(
			DoubleCyclone.netWeapon, arg.pos, arg.xDir, arg.player, arg.netId
		);
	}

	public override void update() {
		base.update();

		if (shootCooldown <= 0) {

			new DoubleCycloneChargedProj(weapon, pos, xDir, damager.owner, damager.owner.getNextActorNetId(), true);
			shootCooldown = 4;
			shootCount++;
		}

		if (shootCooldown > 0) shootCooldown--;
		if (shootCount >= 5) destroySelf();
	}

	public override void onDestroy() {
		base.onDestroy();
		mmx.dCycloneSpawn = null!;
	}
}


public class DoubleCycloneChargedProj : Projectile {
	public DoubleCycloneChargedProj(
		Weapon weapon, Point pos, int xDir,
		Player player, ushort? netProjId,
		bool rpc = false
	) : base(
		weapon, pos, xDir, 180, 1,
		player, "double_cyclone_charged_proj", 0, 0.33f,
		netProjId, player.ownedByLocalPlayer
	) {
		projId = (int)ProjIds.DoubleCycloneCharged;
		destroyOnHit = false;
		maxTime = 1f;
		shouldShieldBlock = false;

		if (rpc) rpcCreate(pos, player, netProjId, xDir);
	}

	public static Projectile rpcInvoke(ProjParameters arg) {
		return new DoubleCycloneChargedProj(
			DoubleCyclone.netWeapon, arg.pos, arg.xDir, arg.player, arg.netId
		);
	}

	public override void onHitDamagable(IDamagable damagable) {
		base.onHitDamagable(damagable);
		var chr = damagable as Character;
		var mav = damagable as Maverick;

		if (chr != null) {
			float mod = 1;
			if (!chr.grounded) mod = 1.5f;
			else if (chr.charState is Crouch) mod = 0.5f;
			
			chr.xPushVel = vel.x * mod;
		}

		if (mav != null) {
			float mod = 0.75f;
			if (!mav.grounded) mod = 1.25f;

			mav.xPushVel = vel.x * mod;
		}
	}
}
