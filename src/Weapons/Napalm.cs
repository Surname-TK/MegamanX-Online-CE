using System;
using System.Collections.Generic;

namespace MMXOnline;

public enum NapalmType {
	NoneBall = -1,
	BumpityBoom,
	RumblingBang,
	SplashHit,
	TerritorialPow,
	BangAwayBomb,
	FlameRound,
	NoneFlamethrower,
}

public class Napalm : Weapon {
	public float vileAmmoUsage;
	public Napalm(NapalmType napalmType) : base() {
		index = (int)WeaponIds.Napalm;
		weaponBarBaseIndex = 0;
		weaponBarIndex = weaponBarBaseIndex;
		killFeedIndex = 30;
		fireRate = 30;
		vileWeight = 0;
		type = (int)napalmType;
		switch (napalmType) {
			case NapalmType.NoneBall:
				displayName = "None(BALL)";
				description = new string[] { "Do not equip a Napalm.", "GRENADE will be used instead." };
				killFeedIndex = 126;
				break;
			case NapalmType.NoneFlamethrower:
				displayName = "None(FLAMETHROWER)";
				description = new string[] { "Do not equip a Napalm.", "FLAMETHROWER will be used instead." };
				killFeedIndex = 126;
				break;
			case NapalmType.BumpityBoom:
				displayName = "Bumpity Boom";
				description = new string[] { "This napalm sports a wide horizontal", "range but cannot attack upward." };
				vileAmmoUsage = 14;
				fireRate = 60;
				vileWeight = 3;
				killFeedIndex = 126;
				break;
			case NapalmType.RumblingBang:
				displayName = "Rumbling Bang";
				description = new string[] { "This napalm sports a wide horizontal", "range but cannot attack upward." };
				vileAmmoUsage = 14;
				fireRate = 60 * 2;
				vileWeight = 3;
				killFeedIndex = 126;
				break;
			case NapalmType.SplashHit:
				displayName = "Splash Hit";
				description = new string[] { "This napalm can attack foes above,", "but has a narrow horizontal range." };
				vileAmmoUsage = 14;
				fireRate = 60 * 3;
				killFeedIndex = 79;
				vileWeight = 3;
				break;
			case NapalmType.TerritorialPow:
				displayName = "Territorial Pow";
				description = new string[] { "This napalm can attack foes above,", "but has a narrow horizontal range." };
				vileAmmoUsage = 14;
				fireRate = 60;
				killFeedIndex = 79;
				vileWeight = 3;
				break;
			case NapalmType.BangAwayBomb:
				displayName = "Bang Away Bomb";
				description = new string[] { "This napalm can attack foes above,", "but has a narrow horizontal range." };
				vileAmmoUsage = 21;
				vileWeight = 3;
				killFeedIndex = 54;
				break;
			case NapalmType.FlameRound:
				displayName = "Flame Round";
				description = new string[] { "This napalm travels along the", "ground, laying a path of fire." };
				vileAmmoUsage = 21;
				fireRate = 60 * 3;
				killFeedIndex = 54;
				vileWeight = 3;
				break;
		}
	}

	public override void vileShoot(WeaponIds weaponInput, Vile vile) {
		if (type == (int)NapalmType.NoneBall || type == (int)NapalmType.NoneFlamethrower) return;
		if (shootCooldown == 0) {
			if (weaponInput == WeaponIds.Napalm) {
				if (vile.tryUseVileAmmo(vileAmmoUsage)) {
					vile.changeState(new NapalmAttack(NapalmAttackType.Napalm), true);
				}
			} else if (weaponInput == WeaponIds.VileFlamethrower) {
				var ground = Global.level.raycast(vile.pos, vile.pos.addxy(0, 25), new List<Type>() { typeof(Wall) });
				if (ground == null) {
					if (vile.tryUseVileAmmo(vileAmmoUsage)) {
						vile.setVileShootTime(this);
						vile.changeState(new AirBombAttack(true), true);
					}
				}
			} else if (weaponInput == WeaponIds.VileBomb) {
				var ground = Global.level.raycast(vile.pos, vile.pos.addxy(0, 25), new List<Type>() { typeof(Wall) });
				if (ground == null) {
					if (vile.player.vileAmmo >= vileAmmoUsage) {
						vile.setVileShootTime(this);
						vile.changeState(new AirBombAttack(true), true);
					}
				}
			}
		}
	}
}

public class NapalmProj : Projectile {
	bool exploded;
	public int type = 0;
	public int bounces = 0;
	public int maxBounces = 1;
	public int bangTime = 0;
	public NapalmProj(
		Napalm weapon, Point pos, int xDir, Player player,
		ushort netProjId, bool rpc = false
	) : base(
		weapon, pos, xDir, 150, 1, player,
		"napalm_grenade", 0, 0.2f, netProjId, player.ownedByLocalPlayer
	) {
		projId = (int)ProjIds.Napalm;
		this.vel = new Point(speed * xDir, -200);
		useGravity = true;
		collider.wallOnly = true;
		maxTime = 2f;
		fadeSound = "explosion";
		fadeSprite = "explosion";
		fadeOnAutoDestroy = true;
		shouldShieldBlock = false;
		type = weapon.type;
		
		switch (weapon.type) {
			case (int)NapalmType.BumpityBoom:
				projId = (int)ProjIds.BumpityBoomNapalm;
				// changeSprite("napalm_bb_grenade", true);
				break;
			case (int)NapalmType.RumblingBang:
				projId = (int)ProjIds.RumblingBangNapalm;
				// changeSprite("napalm_rolbattle_grenade", true);
				break;
			case (int)NapalmType.SplashHit:
				projId = (int)ProjIds.SplashHitNapalm;
				// changeSprite("napalm_sh_grenade", true);
				break;
			case (int)NapalmType.TerritorialPow:
				projId = (int)ProjIds.TerritorialPowNapalm;
				maxTime = 0.25f;
				fadeSprite = "explosion_blue";
				// changeSprite("napalm_tp_grenade", true);
				break;
			case (int)NapalmType.BangAwayBomb:
				projId = (int)ProjIds.BangAwayBombNapalm;
				netcodeOverride = NetcodeModel.FavorDefender;
				damager = new Damager(owner, 1, Global.defFlinch, 0.1f);
				maxTime = 4f;
				maxBounces = 6;
				destroyOnHit = false;
				fadeSprite = "explosion_blue";
				// changeSprite("napalm_bab_grenade", true);
				break;
			case (int)NapalmType.FlameRound:
				projId = (int)ProjIds.FlameRoundNapalm;
				maxBounces = 0;
				// changeSprite("napalm_fr_grenade", true);
				break;
		}
		if (rpc) {
			rpcCreate(pos, player, netProjId, xDir);
		}
		}
	public override void update() {
		base.update();
		if (weapon.type == (int)NapalmType.BangAwayBomb) {
			if (bangTime > 5) {
				bangTime = 0;
				playSound("explosion", false, true);
				new Anim(pos, "explosion_blue", xDir, null, true);
			} else {
				bangTime++;
			}
		}
		/*if (grounded) {
			if (bounces < maxBounces) {
				// vel.y *= -1;
				// bounces++;
			} else explode();
		}*/
	}

	public override void onHitWall(CollideData other) {
		var normal = other.hitData.normal ?? new Point(0, -1);
		if (normal.isSideways()) {
			vel.x *= -1f;
			xDir *= -1;
			incPos(new Point(5 * MathF.Sign(vel.x), 0));
		}
		if (bounces < maxBounces) {
			if (vel.y > 0) vel.y *= -0.9f;
			vel.x *= 0.9f;
			grounded = false;
			bounces++;
		} else {
			explode();
		}
	}

	public override void onHitDamagable(IDamagable damagable) {
		base.onHitDamagable(damagable);
		if (ownedByLocalPlayer && weapon.type != (int)NapalmType.BangAwayBomb) explode();
	}

	public override void onDestroy(){
		base.onDestroy();
		if (weapon.type == (int)NapalmType.TerritorialPow) {
			explode();
		}
	}

	public void explode() {
		if (!ownedByLocalPlayer) return;
		if (exploded) return;
		exploded = true;
		switch (weapon.type) {
			case (int)NapalmType.BumpityBoom:
				new BumpityBoomProj(weapon, pos, xDir, owner, owner.getNextActorNetId(), sendRpc: true);
				break;
			case (int)NapalmType.RumblingBang:
				int[] distances = [-30, 30, -10, 10];
				foreach (int distance in distances) {
					new RumblingBangProj(weapon, pos, xDir, owner, owner.getNextActorNetId(), distance * xDir, rpc: true);
				}
				break;
			case (int)NapalmType.SplashHit:
				var hit = Global.level.raycast(pos.addxy(0, -10), pos.addxy(0, 100), new List<Type>() { typeof(Wall) });
				new SplashHitProj(
					weapon, hit?.getHitPointSafe() ?? pos, xDir,
					owner, owner.getNextActorNetId(), sendRpc: true
				);
				break;
			case (int)NapalmType.TerritorialPow:
				playSound("electricSpark", sendRpc: true);
				//int[] triplets = [-30, 30, -10, 10];
				//foreach (int distance in triplets) {
				new TerritorialPowProj(weapon, new Point(pos.x - (14 * xDir), pos.y - 8), xDir, owner, owner.getNextActorNetId(), sendRpc: true);
				new TerritorialPowProj(weapon, new Point(pos.x + (8 * xDir), pos.y + 14), xDir, owner, owner.getNextActorNetId(), sendRpc: true);
				new TerritorialPowProj(weapon, pos, xDir, owner, owner.getNextActorNetId(), sendRpc: true);
				//}
				break;
			case (int)NapalmType.BangAwayBomb:
				// new BangAwayBombProj(weapon, pos, xDir, owner, owner.getNextActorNetId(), rpc: true);
				break;
			case (int)NapalmType.FlameRound:
				new FlameRoundProj(weapon, pos, xDir, owner, owner.getNextActorNetId(), rpc: true);
				break;
		}
		destroySelf();
	}
}

public enum NapalmAttackType {
	Napalm,
	Ball,
	Flamethrower,
}

public class NapalmAttack : CharState {
	bool shot;
	NapalmAttackType napalmAttackType;
	float shootTime;
	int shootCount;
	Vile vile = null!;

	public NapalmAttack(NapalmAttackType napalmAttackType, string transitionSprite = "") :
		base(getSprite(napalmAttackType), "", "", transitionSprite) {
		this.napalmAttackType = napalmAttackType;
		useDashJumpSpeed = true;
	}

	public static string getSprite(NapalmAttackType napalmAttackType) {
		return napalmAttackType == NapalmAttackType.Flamethrower ? "crouch_flamethrower" : "crouch_nade";
	}

	public override void update() {
		base.update();

		if (napalmAttackType == NapalmAttackType.Napalm) {
			if (!shot && character.sprite.frameIndex == 2) {
				shot = true;
				vile.setVileShootTime(vile.napalmWeapon);
				var poi = character.sprite.getCurrentFrame().POIs[0];
				poi.x *= character.xDir;

				Projectile proj;
				if (napalmAttackType == NapalmAttackType.Napalm) {
					proj = new NapalmProj(vile.napalmWeapon, character.pos.add(poi), character.xDir, character.player, character.player.getNextActorNetId(), rpc: true);
				}
			}
		} else if (napalmAttackType == NapalmAttackType.Ball) {
			if (vile.grenadeWeapon.type == (int)VileBallType.ExplosiveRound) {
				if (shootCount < 3 && character.sprite.frameIndex == 2) {
					if (!vile.tryUseVileAmmo(vile.grenadeWeapon.vileAmmoUsage)) {
						character.changeState(new Crouch(""), true);
						return;
					}
					shootCount++;
					vile.setVileShootTime(vile.grenadeWeapon);
					var poi = character.sprite.getCurrentFrame().POIs[0];
					poi.x *= character.xDir;
					Projectile proj = new VileBombProj(vile.grenadeWeapon, character.pos.add(poi), character.xDir, player, 0, character.player.getNextActorNetId(), rpc: true);
					proj.vel = new Point(character.xDir * 150, -200);
					proj.maxTime = 0.6f;
					character.sprite.frameIndex = 0;
				}
			} else if (vile.grenadeWeapon.type == (int)VileBallType.SpreadShot) {
				shootTime += Global.spf;
				var poi = character.getFirstPOI();
				if (shootTime > 0.06f && poi != null && shootCount <= 4) {
					if (!vile.tryUseVileAmmo(vile.grenadeWeapon.vileAmmoUsage)) {
						character.changeState(new Crouch(""), true);
						return;
					}
					shootTime = 0;
					character.sprite.frameIndex = 1;
					Point shootDir = Point.createFromAngle(-45).times(150);
					if (shootCount == 1) shootDir = Point.createFromAngle(-22.5f).times(150);
					if (shootCount == 2) shootDir = Point.createFromAngle(0).times(150);
					if (shootCount == 3) shootDir = Point.createFromAngle(22.5f).times(150);
					if (shootCount == 4) shootDir = Point.createFromAngle(45f).times(150);
					new StunShotProj(vile.grenadeWeapon, poi.Value, character.xDir, 1, character.player, character.player.getNextActorNetId(), new Point(shootDir.x * character.xDir, shootDir.y), rpc: true);
					shootCount++;
				}
			} else if (vile.grenadeWeapon.type == (int)VileBallType.PeaceOutRoller) {
				if (!shot && character.sprite.frameIndex == 2) {
					if (!vile.tryUseVileAmmo(vile.grenadeWeapon.vileAmmoUsage)) {
						character.changeState(new Crouch(""), true);
						return;
					}
					shot = true;
					vile.setVileShootTime(vile.grenadeWeapon);
					var poi = character.sprite.getCurrentFrame().POIs[0];
					poi.x *= character.xDir;
					Projectile proj = new PeaceOutRollerProj(vile.grenadeWeapon, character.pos.add(poi), character.xDir, player, 0, character.player.getNextActorNetId(), rpc: true);
					proj.vel = new Point(character.xDir * 150, -200);
					proj.gravityModifier = 1;
				}
			}
		} else {
			shootTime += Global.spf;
			var poi = character.getFirstPOI();
			if (shootTime > 0.06f && poi != null) {
				if (!vile.tryUseVileAmmo(2)) {
					character.changeState(new Crouch(""), true);
					return;
				}
				shootTime = 0;
				character.playSound("flamethrower");
				new FlamethrowerProj(vile.flamethrowerWeapon, poi.Value, character.xDir, true, player, player.getNextActorNetId(), sendRpc: true);
			}

			if (character.loopCount > 4) {
				character.changeState(new Crouch(""), true);
				return;
			}
		}

		if (character.isAnimOver()) {
			character.changeState(new Crouch(""), true);
		}
	}

	public override void onEnter(CharState oldState) {
		base.onEnter(oldState);
		vile = character as Vile ?? throw new NullReferenceException();
	}
}

public class BumpityBoomProj : Projectile {
	Player player; // could remove this I guess
	public BumpityBoomProj(Weapon weapon, Point pos, int xDir, Player player, ushort netProjId, bool sendRpc = false) :
		base(weapon, pos, xDir, 0, 2, player, "axl_grenade_explosion2", Global.defFlinch, 0.5f, netProjId, player.ownedByLocalPlayer) {
		projId = (int)ProjIds.BumpityBoom;
		setIndestructableProperties();
		isShield = false;
		fadeOnAutoDestroy = false;
		maxTime = 2.5f;
		this.player = player;

		if (sendRpc) {
			rpcCreate(pos, player, netProjId, xDir);
		}
		}
	public override void update() {
		base.update();
		if (isAnimOver()) destroySelf();
	}
}

public class RumblingBangProj : Projectile {
	float xDist;
	float maxXDist;
	float timeOffset;
	float timeOffset2;
	int secondOffset;
	float napalmPeriod = 0.5f;
	float napalmPeriod2 = 0.2f;
	int firstDir = 1;
	int secondDir = 1;

	public RumblingBangProj(
		Weapon weapon, Point pos, int xDir,
		Player player, ushort netProjId, int xDist, bool rpc = false
	) : base(
		weapon, pos, xDir, 0, 2, player, "napalm_part", 0, 0.5f, netProjId, player.ownedByLocalPlayer
	) {
		projId = (int)ProjIds.RumblingBang;
		if (rpc) {
			rpcCreate(pos, player, netProjId, xDir, (byte)xDist);
		}
		vel.y = -40;
		useGravity = true;
		collider.wallOnly = true;
		destroyOnHit = false;
		shouldShieldBlock = false;
		gravityModifier = 0.25f;
		frameIndex = Helpers.randomRange(0, sprite.totalFrameNum - 1);
		secondOffset = Helpers.randomRange(0, sprite.totalFrameNum - 1);
		timeOffset = Helpers.randomRange(0, 50) / 2;
		timeOffset2 = Helpers.randomRange(0, 50) / 2;
		if (Helpers.randomRange(0, 1) == 1) {
			firstDir = -1;
		}
		if (Helpers.randomRange(0, 1) == 1) {
			secondDir = -1;
		}
		maxXDist = xDist;
		maxTime = 1;
	}

	public override void update() {
		base.update();

		if (useGravity && isUnderwater()) {
			destroySelf(disableRpc: true);
			return;
		}
		if (xDist < MathF.Abs(maxXDist)) {
			float dist = maxXDist / 20 * Global.speedMul;
			xDist += MathF.Abs(dist);
			move(new Point(dist, 0), useDeltaTime: false);
			if (xDist > MathF.Abs(maxXDist)) {
				xDist = MathF.Abs(maxXDist);
			}
		}
		else if (grounded && useGravity) {
			useGravity = false;
			isStatic = true;
		}
	}

	public override void render(float x, float y) {
		if (!shouldRender(x, y)) {
			return;
		}
		float drawX = MathF.Round(pos.x + x);
		float drawY = MathF.Round(pos.y + y) + 1;
		float napalmTime = (time + timeOffset) % napalmPeriod;
		float napalmTime2 = (time + timeOffset2) % napalmPeriod2;
		float separation = 6 * (xDist / MathF.Abs(maxXDist));

		float alpha = MathF.Abs(1 - 2 * (napalmTime / napalmPeriod));
		float alpha2 = MathF.Abs(1 - 2 * (napalmTime2 / napalmPeriod2));
		for (int i = -1; i <= 1; i += 2) {
			int frameToDraw = frameIndex;
			if (i == -1) {
				frameToDraw = (frameIndex + secondOffset) % sprite.totalFrameNum;
			}
			sprite.draw(
				frameToDraw, drawX + i * separation * xDir, drawY, firstDir, yDir,
				getRenderEffectSet(),
				alpha,
				2 - alpha,
				2 - alpha,
				zIndex - 100,
				getShaders(), 0,
				actor: this, useFrameOffsets: true
			);
			sprite.draw(
				frameToDraw, drawX + i * separation * xDir, drawY, secondDir, yDir,
				getRenderEffectSet(),
				1 - alpha,
				1 + alpha2 / 2,
				1 + alpha2 / 2,
				zIndex - 100,
				getShaders(), 0,
				actor: this, useFrameOffsets: true
			);
		}
		renderHitboxes();
	}
}

public class SplashHitProj : Projectile {
	Player player;
	public SplashHitProj(Weapon weapon, Point pos, int xDir, Player player, ushort netProjId, bool sendRpc = false) :
		base(weapon, pos, 1, 0, 1, player, "napalm_sh_proj", 0, 0.5f, netProjId, player.ownedByLocalPlayer) {
		projId = (int)ProjIds.SplashHit;
		setIndestructableProperties();
		maxTime = 1.5f;
		this.player = player;

		if (sendRpc) {
			rpcCreate(pos, player, netProjId, xDir);
		}
	}

	public override void update() {
		base.update();
	}

	public override bool shouldDealDamage(IDamagable damagable) {
		if (damagable is Actor actor && MathF.Abs(pos.x - actor.pos.x) > 40) {
			return false;
		}
		return true;
	}

	public override void onHitDamagable(IDamagable damagable) {
		base.onHitDamagable(damagable);
		if (damagable is Character chr) {
			float modifier = 1;
			if (chr.isUnderwater()) modifier = 2;
			if (chr.isImmuneToKnockback()) return;
			float xMoveVel = MathF.Sign(pos.x - chr.pos.x);
			chr.move(new Point(xMoveVel * 50 * modifier, 0));
		}
	}
}

public class TerritorialPowProj : Projectile {
	Player player; // could remove this I guess
	public TerritorialPowProj(Weapon weapon, Point pos, int xDir, Player player, ushort netProjId, bool sendRpc = false) :
		base(weapon, pos, xDir, 0, 2, player, "napalm_tp_proj", 0, 1f, netProjId, player.ownedByLocalPlayer) {
		projId = (int)ProjIds.TerritorialPow;
		setIndestructableProperties();
		isShield = true;
		shouldDing = false; // this did not work
		fadeSprite = "rakuhouha_fade";
		fadeOnAutoDestroy = true;
		maxTime = 2.5f;
		this.player = player;

		if (sendRpc) {
			rpcCreate(pos, player, netProjId, xDir);
		}
		}
}

public class FlameRoundProj : Projectile {
	float flameCreateTime = 1;
	public FlameRoundProj(Weapon weapon, Point pos, int xDir, Player player, ushort netProjId, bool rpc = false) :
		base(weapon, pos, xDir, 0, 1f, player, "napalm2_proj", 0, 1f, netProjId, player.ownedByLocalPlayer) {
		maxTime = 1f;
		projId = (int)ProjIds.FlameRound;
		useGravity = true;
		collider.wallOnly = true;
		destroyOnHit = false;
		shouldShieldBlock = false;

		if (rpc) {
			rpcCreate(pos, player, netProjId, xDir);
		}
	}

	public override void update() {
		base.update();
		if (ownedByLocalPlayer) {
			flameCreateTime += Global.spf;
			if (flameCreateTime > 0.1f) {
				flameCreateTime = 0;
				// new Anim(pos, "napalm2_flame", xDir, owner.getNextActorNetId());
				new FlameRoundTrail(weapon, pos, xDir, owner, owner.getNextActorNetId(), rpc: true);
			}
		}
		if (time > 0.2f) {
			vel.x = 100 * xDir;
		}
		var hit = Global.level.checkTerrainCollisionOnce(this, vel.x * Global.spf, 0, null);
		if (hit?.gameObject is Wall && hit?.hitData?.normal != null && !(hit.hitData.normal.Value.isAngled())) {
			if (ownedByLocalPlayer) {
				new RisingFlameRoundProj(weapon, pos, xDir, owner, owner.getNextActorNetId(), rpc: true);
			}
			destroySelf();
		}
	}
}

public class FlameRoundTrail : Projectile {
	public FlameRoundTrail(Weapon weapon, Point pos, int xDir, Player player, ushort netProjId, bool rpc = false) :
		base(weapon, pos, xDir, 0, 0, player, "napalm2_flame", 0, 1f, netProjId, player.ownedByLocalPlayer) {
		projId = (int)ProjIds.FlameRoundTrail;
		if (rpc) {
			rpcCreate(pos, player, netProjId, xDir);
		}
		useGravity = true;
		collider.wallOnly = true;
		destroyOnHit = true;
		shouldShieldBlock = false;
		gravityModifier = 0.25f;
	}

	public override void update() {
		base.update();
		if (isUnderwater()) {
			destroySelf(disableRpc: true);
			return;
		}
		if (loopCount > 8) {
			destroySelf(disableRpc: true);
			return;
		}
	}
	public override void onHitDamagable(IDamagable damagable) {
		base.onHitDamagable(damagable);
	}
}

public class RisingFlameRoundProj : Projectile {
	public RisingFlameRoundProj(Weapon weapon, Point pos, int xDir, Player player, ushort netProjId, bool rpc = false) :
		base(weapon, pos, xDir, 0, 2, player, "napalm2_wall", 0, 0.5f, netProjId, player.ownedByLocalPlayer) {
		maxTime = 1f;
		projId = (int)ProjIds.RisingFlameRound;
		vel = new Point(0, -200);
		destroyOnHit = false;
		shouldShieldBlock = false;
		if (rpc) {
			rpcCreate(pos, player, netProjId, xDir);
		}
	}

	public override void update() {
		base.update();
		if (isUnderwater()) {
			destroySelf(disableRpc: true);
		}
	}
}