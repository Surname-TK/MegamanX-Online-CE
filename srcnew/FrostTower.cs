using System;
using System.Collections.Generic;

namespace MMXOnline;

public class FrostTower : Weapon {

	public static FrostTower netWeapon = new();
	public FrostTower()
	{
		shootSounds = new string[] { "frostTower", "frostTower", "frostTower", "" };
		fireRate = 90;
		switchCooldownFrames = 21;
		index = (int)WeaponIds.FrostTower;
		weaponBarIndex = 62;
		weaponBarBaseIndex = 73;
		weaponSlotIndex = 124;
		killFeedIndex = 184;
		weaknessIndex = (int)WeaponIds.RisingFire;
		hasCustomAnim = true;
		/* damage = "1-2/3";
		hitcooldown = "0.75/0.5";
		Flinch = "0-13/26";
		FlinchCD = hitcooldown;
		effect = "Blocks projectiles. C: Summons huge icicles that drop from above."; */
	}

	public override void shoot(Character character, int[] args) {
		int chargeLevel = args[0];

		if (chargeLevel < 3) {
			character.changeState(new FrostTowerState(), true);
		}
		else character.changeState(new FrostTowerChargedState(), true);
	}
}

public class FrostTowerState : CharState {

	bool fired;
	public FrostTowerState() : base("summon") {
		attackCtrl = false;
		normalCtrl = false;
		useGravity = false;
		useDashJumpSpeed = true;
	}

	public override void onEnter(CharState oldState) {
		base.onEnter(oldState);
		character.stopMoving();
	}

	public override void update() {
		base.update();

		if (!fired && character.frameIndex == 1) {
			Point shootPos = character.getCenterPos();
			int xDir = character.getShootXDir();
			Player player = character.player;

			new FrostTowerProj(new FrostTower(), shootPos, xDir, player, player.getNextActorNetId(), true);
			fired = true;
		}

		if (character.isAnimOver()) character.changeToIdleOrFall();
	}
}

public class FrostTowerProj : Projectile, IDamagable

{
	public float health = 4;

	public float maxHealth = 4;

	public bool landed;
	float zTime;
	int zMul = -1;

	public FrostTowerProj(
		Weapon weapon, Point pos, int xDir, 
		Player player, ushort netProjId, bool rpc = false
	) : base(
		weapon, pos, xDir, 0, 1, player, "frosttower_proj", 
		0, 0.5f, netProjId, player.ownedByLocalPlayer
	) {
		maxTime = 2.5f;
		projId = (int)ProjIds.FrostTower;
		grounded = false;
		canBeGrounded = true;
		isShield = true;
		destroyOnHit = false;
		shouldShieldBlock = false;
		collider.isClimbable = true;
		zIndex = ZIndex.MainPlayer + 10;
		
		if (rpc) rpcCreate(pos, player, netProjId, xDir);
	}

	public static Projectile rpcInvoke(ProjParameters arg) {
		return new FrostTowerProj(
			FrostTower.netWeapon, arg.pos, arg.xDir, arg.player, arg.netId
		);
	}

	public override void update() {
		base.update();
		if (sprite.frameIndex >= 3) useGravity = true;
		updateProjectileCooldown();

		if (landed && ownedByLocalPlayer) moveWithMovingPlatform();

		if (!grounded && MathF.Abs(vel.y) > 60) updateDamager(Helpers.clamp(MathF.Floor(deltaPos.y * 0.6f), 1, 4), Global.halfFlinch);
		else updateDamager(1, 0);

		zIndex = zTime % 2 == 0 ? ZIndex.MainPlayer + 10 : ZIndex.Character - 10;
		zTime += Global.speedMul;
		
		if (!ownedByLocalPlayer || base.owner == null || landed) return;
	}
	public void applyDamage(float damage, Player? owner, Actor? actor, int? weaponIndex, int? projId) {
		health -= damage;
		bool isRisingFire =
			projId == (int)ProjIds.RisingFire ||
			projId == (int)ProjIds.RisingFireCharged ||
			projId == (int)ProjIds.RisingFireChargedStart ||
			projId == (int)ProjIds.RisingFireUnderwater ||
			projId == (int)ProjIds.RisingFireUnderwaterCharged;

		if (health <= 0 || isRisingFire) destroySelf();
	}

	public bool canBeDamaged(int damagerAlliance, int? damagerPlayerId, int? projId) {
		return base.owner.alliance != damagerAlliance;
	}

	public bool isInvincible(Player attacker, int? projId) {
		return false;
	}

	public bool canBeHealed(int healerAlliance) {
		return false;
	}

	public void heal(Player healer, float healAmount, bool allowStacking = true, bool drawHealText = false) {}

	public override void onDestroy() {
		base.onDestroy();
		breakFreeze(owner);
	}
}


public class FrostTowerChargedState : CharState {

	bool fired;
	int lap = 1;
	float cooldown = 15;
	Point spawnPos;
	float extraPos;
	float p = 48;

	public FrostTowerChargedState() : base("summon") {
		normalCtrl = false;
		attackCtrl = false;
		useDashJumpSpeed = true;
		useGravity = false;
	}

	public override void onEnter(CharState oldState) {
		base.onEnter(oldState);
		character.stopMoving();
		spawnPos = character.getCenterPos().addxy(0, -96);
	}

	public override void onExit(CharState newState) {
		base.onExit(newState);
		MegamanX mmx = character as MegamanX ?? throw new NullReferenceException();
		mmx.shootCooldown = 60;
	}

	public override void update() {
		base.update();

	
		int xDir = character.getShootXDir();

		if (character.frameIndex >= 1) {
			Helpers.decrementFrames(ref cooldown);
		}

		if (cooldown <= 0) {
			if (lap > 4) character.changeToIdleOrFall();
			else shoot(spawnPos, xDir, lap);
		} 
	}

	void shoot(Point pos, int xDir, int l) {
		for (int i = 0; i < l; i++) {
			float extra = extraPos + (i * p * 2);
			new FrostTowerProjCharged(new FrostTower(), pos.addxy(extra, 0), xDir, player, player.getNextActorNetId(), true) 
			{ canReleasePlasma = player.hasPlasma() && l == 1 };
			character.playSound("frostTower", sendRpc: true);
		}
		character.shakeCamera(true);
		cooldown = 45;
		extraPos -= p;
		lap++;
	} 
}

public class FrostTowerProjCharged : Projectile, IDamagable {
	public float health = 4;
	public float maxHealth = 4;

	public bool canReleasePlasma;

	public FrostTowerProjCharged(
		Weapon weapon, Point pos, int xDir, 
		Player player, ushort netProjId, bool rpc = false
	) : base(
		weapon, pos, xDir, 0, 2, player, "frosttowercharged_proj", 
		Global.defFlinch, 0.5f, netProjId, player.ownedByLocalPlayer
	) {
		maxTime = 2f;
		projId = (int)ProjIds.FrostTowerCharged;
		isShield = false;
		
		if (rpc) rpcCreate(pos, player, netProjId, xDir);
	}

	public static Projectile rpcInvoke(ProjParameters arg) {
		return new FrostTowerProjCharged(
			FrostTower.netWeapon, arg.pos, arg.xDir, arg.player, arg.netId
		);
	}

	public override void update() {
		base.update();
		if (isAnimOver()) useGravity = true;
		
	}
	
	public void applyDamage(float damage, Player? owner, Actor? actor, int? weaponIndex, int? projId) {
		health -= damage;
		bool isRisingFire =
			projId == (int)ProjIds.RisingFire ||
			projId == (int)ProjIds.RisingFireCharged ||
			projId == (int)ProjIds.RisingFireChargedStart ||
			projId == (int)ProjIds.RisingFireUnderwater ||
			projId == (int)ProjIds.RisingFireUnderwaterCharged;
			
		if (health <= 0 || isRisingFire) destroySelf();
	}
	public bool canBeDamaged(int damagerAlliance, int? damagerPlayerId, int? projId) {
		return base.owner.alliance != damagerAlliance;
	}
	public bool isInvincible(Player attacker, int? projId) {
		return false;
	}
	public bool canBeHealed(int healerAlliance) {
		return false;
	}
	public void heal(Player healer, float healAmount, bool allowStacking = true, bool drawHealText = false) {}

	
	/*public override void onHitWall(CollideData other) {
		base.onHitWall(other);

		/* if (other.isCeilingHit()) return;
		else if (other.isSideWallHit()) return;
		else if (other.isGroundHit()) destroySelf(); 
	}*/

	public override void onHitDamagable(IDamagable damagable) {
		base.onHitDamagable(damagable);

		if (canReleasePlasma && !hasReleasedPlasma) {
			new BusterForcePlasmaHit(
				6, weapon, pos, xDir, damager.owner,
				damager.owner.getNextActorNetId(), true
			);
			hasReleasedPlasma = true;
		}
	}

	public override void onDestroy() {
		base.onDestroy();
		breakFreeze(owner);
		
		if (canReleasePlasma && !hasReleasedPlasma) {
			new BusterForcePlasmaHit(
				6, weapon, pos, xDir, damager.owner,
				damager.owner.getNextActorNetId(), true
			);
			hasReleasedPlasma = true;
		}
	}
}

public class FrostTowerProjChargedMini : Projectile {
	public FrostTowerProjChargedMini(
		Weapon weapon, Point pos, int xDir, 
		Player player, ushort netProjId, bool rpc = false
	) : base(
		weapon, pos, xDir, 300f, 2, player, "frosttowercharged_proj_mini1", 
		Global.halfFlinch, 1f, netProjId, player.ownedByLocalPlayer)
	{
		maxTime = 3f;
		projId = 411;
		isShield = true;
		useGravity = true;
		destroyOnHit = true;
		vel.y = -100;
		if (rpc)
		{
			rpcCreate(pos, player, netProjId, xDir);
		}
	}

	public override void update() {	
		base.update();
	}
	
	public override void onHitWall(CollideData other) {
		base.onHitWall(other);
		destroySelf();
	}

	public override void onDestroy() {
		base.onDestroy();
		breakFreeze(owner);
		int offsetX = 12 * xDir;
		int offsetX2 = -12 * xDir;
		Point adjustedPos = pos.addxy(offsetX, 0);
		Point adjustedPos2 = pos.addxy(offsetX2, 0);
		if (ownedByLocalPlayer)
		{
			new FrostTowerProjChargedMini2(weapon, adjustedPos, -xDir, base.owner, base.owner.getNextActorNetId(), rpc: true);
			new FrostTowerProjChargedMini2(weapon, adjustedPos2, xDir, base.owner, base.owner.getNextActorNetId(), rpc: true);
		}
	}
}


public class FrostTowerProjChargedMini2 : Projectile

{
	public FrostTowerProjChargedMini2(Weapon weapon, Point pos, int xDir, Player player, ushort netProjId, bool rpc = false)
		: base(weapon, pos, xDir, 200f, 1, player, "frosttowercharged_proj_mini2", 6, 1f, netProjId, player.ownedByLocalPlayer)
	{
		maxTime = 3f;
		projId = 412;
		isShield = true;
		useGravity = true;
		destroyOnHit = true;
		vel.y = -100;
		if (rpc)
		{
			rpcCreate(pos, player, netProjId, xDir);
		}
	}

	public override void update()
	{
		base.update();
	}
	
	public override void onHitWall(CollideData other)
	{
		base.onHitWall(other);
		destroySelf();
	}

	public override void onDestroy() {
		base.onDestroy();
		//breakFreeze(owner);
		int offsetX = 12 * xDir;
		int offsetX2 = -12 * xDir;
		Point adjustedPos = pos.addxy(offsetX, 0);
		Point adjustedPos2 = pos.addxy(offsetX2, 0);
		if (ownedByLocalPlayer)
		{
			new FrostTowerProjChargedMini3(weapon, adjustedPos, -xDir, base.owner, base.owner.getNextActorNetId(), rpc: true);
			new FrostTowerProjChargedMini3(weapon, adjustedPos2, xDir, base.owner, base.owner.getNextActorNetId(), rpc: true);
		}
	}
}

public class FrostTowerProjChargedMini3 : Projectile

{
	public FrostTowerProjChargedMini3(Weapon weapon, Point pos, int xDir, Player player, ushort netProjId, bool rpc = false)
		: base(weapon, pos, xDir, 100f, 0, player, "frosttowercharged_proj_mini3", 0, 1f, netProjId, player.ownedByLocalPlayer)
	{
		maxTime = 3f;
		projId = 413;
		isShield = true;
		useGravity = true;
		destroyOnHit = true;
		vel.y = -100;
		if (rpc)
		{
			rpcCreate(pos, player, netProjId, xDir);
		}
	}

	public override void update()
	{
		base.update();
	}
	
	public override void onHitWall(CollideData other)
	{
		base.onHitWall(other);
		destroySelf();
	}

	public override void onDestroy() {
	//	base.onDestroy();
		breakFreeze(owner);
	}
}

public class FrostTowerRainState : CharState
{
	private bool fired;

	private float spikeTime;

	private float endTime;

	private int state;

	public FrostTowerRainState()
		: base("summon", "", "", "")
	{
		superArmor = true;
	}

	public override void update()
	{
		base.update();
		if (base.player == null)
		{
			return;
		}
		if (state == 0)
		{
			if (stateTime > 0.25f)
			{
				state = 1;
			}
		}
		else if (state == 1)
		{
			spikeTime += Global.spf;
			if (spikeTime > 0.075f)
			{
				spikeTime = 0f;
				int offsetY = 30;
				float randY = offsetY;
				float randX = Helpers.randomRange(-150, 150);
				new FrostTowerRainProj(base.player.weapon, pos: new Point (character.pos.x + randX, randY), xDir: 1, player: base.player, netProjId: base.player.getNextActorNetId(), rpc: true);
				character.shakeCamera(sendRpc: false);
				//character.playSound("frostTower", forcePlay: false, sendRpc: true);
			}
			if (stateTime >= 4)
			{
				state = 2;
			}
		}
		else if (state == 2)
		{
			endTime += Global.spf;
			if (endTime > 0.25f)
			{
				character.changeToIdleOrFall();
			}
		}
	}

	public override void onEnter(CharState oldState)
	{
		base.onEnter(oldState);
		character.useGravity = false;
		character.vel = default(Point);
	}

	public override void onExit(CharState newState)
	{
		base.onExit(newState);
		character.useGravity = true;
	}
}

public class FrostTowerRainProj : Projectile

{
	public FrostTowerRainProj(Weapon weapon, Point pos, int xDir, Player player, ushort netProjId, bool rpc = false)
		: base(weapon, pos, xDir, 0f, 0, player, "frosttowercharged_proj_mini1", 0, 0f, netProjId, player.ownedByLocalPlayer)
	{
		projId = 414;
		isShield = true;
		useGravity = true;
		destroyOnHit = true;
		vel.y = 100;
		checkUnderwater();
		if (rpc)
		{
			rpcCreate(pos, player, netProjId, xDir);
		}
	}

	public override void update()
	{
		base.update();
	}
	public void checkUnderwater()
	{
		if (isUnderwater())
		{
			useGravity = false;
		}
	}
}
