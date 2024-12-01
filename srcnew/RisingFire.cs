using System.Collections.Generic;

namespace MMXOnline;

public class RisingFire : Weapon {

	public static RisingFire netWeapon = new();

	public RisingFire() {
		shootSounds = new string[] { "ryuenjin", "ryuenjin", "ryuenjin", "ryuenjin" };
		fireRate = 45;
		index = (int)WeaponIds.RisingFire;
		weaponBarIndex = 64;
		weaponBarBaseIndex = 75;
		weaponSlotIndex = 126;
		killFeedIndex = 183;
		weaknessIndex = (int)WeaponIds.DoubleCyclone;
		hasCustomAnim = true;
		/* damage = "2+1-1/2+1-1";
		hitcooldown = "0.5";
		Flinch = "0/13-26";
		FlinchCD = hitcooldown;
		effect = "Burns upper enemies. C: Resets airdashes count."; */
	}

	public override void shoot(Character character, int[] args) {
		int chargeLevel = args[0];

		if (chargeLevel < 3f) {
			character.changeState(new RisingFireState(), true);	
		} else {
			character.changeState(new RisingFireChargedState(), true);
		}
	}
}


public class RisingFireState : CharState {
	private bool fired;

	public RisingFireState()
		: base("risingfire")
	{
		superArmor = false;
	}

	public override void update() {
		base.update();
		
		if (character.currentFrame.getBusterOffset() != null && !fired) {
			Point shootPos = character.getFirstPOI() ?? character.getShootPos();
			int xDir = character.getShootXDir();
			Player player = character.player;

			if (!character.isUnderwater()) {
				new RisingFireProj(new RisingFire(), shootPos, xDir, player, player.getNextActorNetId(), true);
			} else {
				new RisingFireWaterProj(new RisingFire(), shootPos, xDir, player, player.getNextActorNetId(), true);
			}
			
			fired = true;
		}

		if (character.isAnimOver()) character.changeToIdleOrFall();
	}

	public override void onEnter(CharState oldState) {
		base.onEnter(oldState);
		character.vel = new Point();
		character.useGravity = false;
		
		bool air = !character.grounded || character.vel.y < 0;
		defaultSprite = sprite;
		landSprite = "risingfire";
		if (air) {
			sprite = "risingfire_air";
			defaultSprite = sprite;
		}
		character.changeSpriteFromName(sprite, true);
	}

	public override void onExit(CharState newState) {
		base.onExit(newState);
		character.useGravity = true;
	}
}

public class RisingFireProj : Projectile {
	public RisingFireProj(
		Weapon weapon, Point pos, int xDir, 
		Player player, ushort netProjId, bool rpc = false
	) : base(
		weapon, pos, xDir, 0, 2, player, "risingfire_proj", 
		0, 0.5f, netProjId, player.ownedByLocalPlayer
	) {
		maxTime = 0.6f;
		projId = (int)ProjIds.RisingFire;
		shouldShieldBlock = false;
		shouldVortexSuck = false;
		destroyOnHit = false;
		vel.y = -275;
		
		
		if (rpc) rpcCreate(pos, player, netProjId, xDir);
	}

	public static Projectile rpcInvoke(ProjParameters arg) {
		return new RisingFireProj(
			RisingFire.netWeapon, arg.pos, arg.xDir, arg.player, arg.netId
		);
	}

	public override void update() {
		base.update();

		if (isUnderwater()) destroySelf();
	}
}

public class RisingFireChargedState : CharState {
	private bool jumpedYet;

	private float timeInWall;

	private Projectile? proj;

    public RisingFireChargedState()
		: base("risingfire_charged") {
	
	}

	public override void update() {
		base.update();
		
		int xDir = character.xDir;
		Point pos = character.pos;
		Player player = character.player;
		Point shootPos = character.getShootPos();

		if (character.sprite.frameIndex >= 2 && !jumpedYet) {
			jumpedYet = true;
			character.vel.y = -character.getJumpPower();
		}
		
		if (character.vel.y < 0) character.move(new Point(character.xDir * 165, 0f));

		if (character.currentFrame.getBusterOffset() != null) {
			Point poi = character.currentFrame.POIs[0];
			Point firePos = character.pos.addxy(poi.x * (float)character.xDir, poi.y);

			if (proj == null) {

				if (!character.isUnderwater()){
					proj = new RisingFireProjChargedStart(new RisingFire(), pos, xDir, player, player.getNextActorNetId(), true);
				} else {
					proj = new RisingFireProjChargedStart(new RisingFire(), pos, xDir, player, player.getNextActorNetId(), true);
				}
				proj.releasePlasma = player.hasPlasma();
			}
			
			else proj.changePos(firePos);
			
		}
		else if (character.sprite.frameIndex == 3 && proj != null) {
			proj.destroySelf();
			proj = null!;
		}
		
		CollideData? wallAbove = Global.level.checkTerrainCollisionOnce(character, 0, -10);
		
		if (wallAbove != null && wallAbove.gameObject is Wall) {
			timeInWall++;
			if (timeInWall > 6) {
				character.vel.y = 1;
				character.changeState(new Fall());
				return;
			}
		}
		if (character.isAnimOver()) {
			character.changeState(new Fall());

			Projectile? rf;
			if (!character.isUnderwater()) {
				rf = new RisingFireProjCharged(
					new RisingFire(), shootPos, xDir, player, player.getNextActorNetId(), rpc: true);
				} else {
				rf = new RisingFireWaterProjCharged(
					new RisingFire(), shootPos, xDir, player, player.getNextActorNetId(), rpc: true);
			}
			
			if (proj != null && proj.releasePlasma && !proj.hasReleasedPlasma && rf != null) {
				rf.releasePlasma = true;
			}
		}
	}

	public override void onEnter(CharState oldState) {
		base.onEnter(oldState);
		character.dashedInAir = 0;
	}
	

	public override void onExit(CharState newState) {
		base.onExit(newState);
		if (proj != null) proj.destroySelf();
	}
}

public class RisingFireProjChargedStart : Projectile {
	public RisingFireProjChargedStart(
		Weapon weapon, Point pos, int xDir, 
		Player player, ushort netProjId, bool rpc = false
	) : base(
		weapon, pos, xDir, 0f, 2f, player, "risingfire_proj_charged",
		Global.halfFlinch, 0.5f, netProjId, player.ownedByLocalPlayer
	) {
		maxTime = 0.6f;
		projId = (int)ProjIds.RisingFireChargedStart;
		shouldShieldBlock = false;
		destroyOnHit = false;
		shouldVortexSuck = false;
		canBeLocal = false;
		
		if (rpc) rpcCreate(pos, player, netProjId, xDir);
	}

	public static Projectile rpcInvoke(ProjParameters arg) {
		return new RisingFireProjChargedStart(
			RisingFire.netWeapon, arg.pos, arg.xDir, arg.player, arg.netId
		);
	}

	public override void update() {
		base.update();

		if (isUnderwater()) destroySelf();
	}
}


public class RisingFireProjCharged : Projectile {
	public RisingFireProjCharged(
		Weapon weapon, Point pos, int xDir, 
		Player player, ushort netProjId, bool rpc = false
	) : base(
		weapon, pos, xDir, 0, 2, player, "risingfire_proj_charged", 
		Global.defFlinch, 0.5f, netProjId, player.ownedByLocalPlayer
	) {
		maxTime = 0.6f;
		projId = (int)ProjIds.RisingFireCharged;
		shouldShieldBlock = false;
		shouldVortexSuck = false;
		vel.y = -275;
		if (isUnderwater()) destroySelf();

		if (rpc) rpcCreate(pos, player, netProjId, xDir);
	}

	public static Projectile rpcInvoke(ProjParameters arg) {
		return new RisingFireProjCharged(
			RisingFire.netWeapon, arg.pos, arg.xDir, arg.player, arg.netId
		);
	}

	public override void update() {
		base.update();
		if (isUnderwater()) destroySelf();
	}
}

public class RisingFireWaterProj : Projectile {
	public RisingFireWaterProj(
		Weapon weapon, Point pos, int xDir, 
		Player player, ushort netProjId, bool rpc = false
	) : base(
		weapon, pos, xDir, 0, 1, player, "risingfire_proj_water", 
		0, 0.5f, netProjId, player.ownedByLocalPlayer
	) {
		maxTime = 0.6f;
		projId = (int)ProjIds.RisingFireUnderwater;
		shouldShieldBlock = false;
		shouldVortexSuck = false;
		vel.y = -275;
		if (!isUnderwater()) destroySelf();
		
		if (rpc) rpcCreate(pos, player, netProjId, xDir);
	}
	
	public static Projectile rpcInvoke(ProjParameters arg) {
		return new RisingFireWaterProj(
			RisingFire.netWeapon, arg.pos, arg.xDir, arg.player, arg.netId
		);
	}

	public override void update() {
		base.update();
		if (!isUnderwater()) destroySelf();
	}
}

public class RisingFireWaterProjCharged : Projectile {
	public RisingFireWaterProjCharged(
		Weapon weapon, Point pos, int xDir, 
		Player player, ushort netProjId, bool rpc = false
	) : base(
		weapon, pos, xDir, 0, 1, player, "risingfire_proj_water", 
		0, 0.5f, netProjId, player.ownedByLocalPlayer
	) {
		maxTime = 0.6f;
		projId = (int)ProjIds.RisingFireUnderwaterCharged;
		shouldShieldBlock = false;
		shouldVortexSuck = false;
		vel.y = -275;
		if (!isUnderwater()) destroySelf();
		
		if (rpc) rpcCreate(pos, player, netProjId, xDir);
	}

	public static Projectile rpcInvoke(ProjParameters arg) {
		return new RisingFireWaterProjCharged(
			RisingFire.netWeapon, arg.pos, arg.xDir, arg.player, arg.netId
		);
	}

	public override void update() {
		base.update();
		if (!isUnderwater()) destroySelf();
	}
}
