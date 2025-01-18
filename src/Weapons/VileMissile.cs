using System;

namespace MMXOnline;

public enum VileMissileType {
	None = -1,
	ElectricShock,
	HumerusCrush,
	PopcornDemon,
	BanzaiBeetle,
	LostLamb,
	SerotinalBullet
}

public class VileMissile : Weapon {
	public static VileMissile netWeaponHC = new VileMissile(VileMissileType.HumerusCrush);
	public static VileMissile netWeaponPD = new VileMissile(VileMissileType.PopcornDemon);
	public static VileMissile netWeaponBB = new VileMissile(VileMissileType.BanzaiBeetle);
	public static VileMissile netWeaponLL = new VileMissile(VileMissileType.LostLamb);
	public static VileMissile netWeaponSB = new VileMissile(VileMissileType.SerotinalBullet);
	public string projSprite = "";
	public float vileAmmo;

	public VileMissile(VileMissileType vileMissileType) : base() {
		index = (int)WeaponIds.ElectricShock;
		weaponBarBaseIndex = 26;
		weaponBarIndex = weaponBarBaseIndex;
		weaponSlotIndex = 42;
		killFeedIndex = 17;
		type = (int)vileMissileType;
		switch (vileMissileType){
			case VileMissileType.None:
				displayName = "None";
				description = new string[] { "Do not equip a Missile." };
				vileAmmo = 7;
				killFeedIndex = 126;
				break;
			case VileMissileType.ElectricShock:
				fireRate = 60;
				displayName = "Electric Shock";
				vileAmmo = 14;
				description = new string[] { "Stops enemies in their tracks,", "but deals no damage." };
				vileWeight = 3;
				break;
			case VileMissileType.HumerusCrush:
				fireRate = 40;
				displayName = "Humerus Crush";
				projSprite = "missile_hc_proj";
				vileAmmo = 7;
				description = new string[] { "This missile shoots straight", "and deals decent damage." };
				killFeedIndex = 74;
				vileWeight = 3;
				break;
			case VileMissileType.PopcornDemon:
				fireRate = 30;
				displayName = "Popcorn Demon";
				projSprite = "missile_pd_proj";
				vileAmmo = 14;
				description = new string[] { "This missile splits into 3", "and can cause great damage." };
				killFeedIndex = 76;
				vileWeight = 3;
				break;
			case VileMissileType.BanzaiBeetle:
				fireRate = 30;
				displayName = "Banzai Beetle";
				projSprite = "missile_bb_proj";
				vileAmmo = 14;
				description = new string[] { "A set of wings allows this missile", "to glide, contacting many enemies." };
				killFeedIndex = 185;
				vileWeight = 3;
				break;
			case VileMissileType.LostLamb:
				fireRate = 10;
				displayName = "Lost Lamb";
				projSprite = "missile_ll_proj";
				vileAmmo = 7;
				description = new string[] { "This missile travels at an odd", "angle but can be very useful." };
				killFeedIndex = 186;
				vileWeight = 3;
				break;
			case VileMissileType.SerotinalBullet:
				fireRate = 10;
				displayName = "Serotinal Bullet";
				projSprite = "missile_sb_proj";
				vileAmmo = 7;
				description = new string[] { "This missile is extremely slow,", "but can be set as a trap." };
				killFeedIndex = 187;
				vileWeight = 3;
				break;
		}
	}

	public override void vileShoot(WeaponIds weaponInput, Vile vile) {
		Player player = vile.player;
		if (shootCooldown > 0) return;

		if (vile.charState is Idle || vile.charState is Run || vile.charState is Crouch) {
			if (vile.tryUseVileAmmo(vileAmmo)) {
				if (!vile.isVileMK2) {
					vile.setVileShootTime(this);
					vile.changeState(new MissileAttack(), true);
				} else if (!vile.charState.isGrabbing) {
					vile.setVileShootTime(this);
					MissileAttack.mk2ShootLogic(vile, vile.missileWeapon.type == (int)VileMissileType.ElectricShock);
				}
			}
		} else if (vile.charState is InRideArmor) {
			if (!vile.isVileMK2) {
				vile.setVileShootTime(this);
				if (vile.missileWeapon.type == 2 || vile.missileWeapon.type == 1) {
					vile.playSound("vileMissile", sendRpc: true);
					new VileMissileProj(vile.missileWeapon, vile.getFirstPOIOrDefault(), vile.getShootXDir(), 0, vile.player, vile.player.getNextActorNetId(), new Point(vile.xDir, 0), rpc: true);
				} else {
					new StunShotProj(this, vile.pos.addxy(15 * vile.xDir, -10), vile.getShootXDir(), 0, player, player.getNextActorNetId(), vile.getVileShootVel(true), rpc: true);
				}
			} else {
				vile.setVileShootTime(this);
				if (vile.missileWeapon.type == 2 || vile.missileWeapon.type == 1) {
					vile.playSound("mk2stunshot", sendRpc: true);
					new VileMissileProj(vile.missileWeapon, vile.getFirstPOIOrDefault(), vile.getShootXDir(), 0, vile.player, vile.player.getNextActorNetId(), new Point(vile.xDir, 0), rpc: true);
				} else {
					MissileAttack.mk2ShootLogic(vile, true);
				}
			}
		}
	}
}

public class VileMissileProj : Projectile {
	public VileMissile missileWeapon;
	bool split;
	int type;
	public VileMissileProj(VileMissile weapon, Point pos, int xDir, int type, Player player, ushort netProjId, Point? vel = null, bool rpc = false) :
		base(weapon, pos, xDir, 200, 3, player, weapon.projSprite, 0, 0, netProjId, player.ownedByLocalPlayer) {
		fadeSprite = "explosion";
		fadeSound = "explosion";
		projId = (int)ProjIds.VileMissile;
		maxTime = 0.6f;
		destroyOnHit = true;
		destroyOnHitWall = true;
		fadeOnAutoDestroy = true;
		missileWeapon = weapon;
		reflectableFBurner = true;
		this.type = type;
		canBeLocal = false; // TODO: Remove the need for this.
		
		switch (weapon.type) {
			case (int)VileMissileType.HumerusCrush:
				projId = (int)ProjIds.HumerusCrush;
				damager.damage = 3;
				this.vel.x = xDir * 350;
				maxTime = 0.35f;
				break;
			case (int)VileMissileType.PopcornDemon:
				projId = (int)ProjIds.PopcornDemon;
				damager.damage = 2;
				if (type == 1) {
					projId = (int)ProjIds.PopcornDemonSplit;
					this.xDir = 1;
					this.vel = vel.Value.times(speed);
					angle = this.vel.angle;
					damager.damage = 1;
					damager.hitCooldown = 0;
				}
				break;
			case (int)VileMissileType.BanzaiBeetle:
				projId = (int)ProjIds.BanzaiBeetle;
				damager = new Damager(owner, 2, 0, 0.25f);
				destroyOnHit = false;
				shouldShieldBlock = false;
				this.vel.x = xDir * 200;
				maxTime = 0.6f;
				maxDistance = 200;
				break;
			case (int)VileMissileType.LostLamb:
				projId = (int)ProjIds.LostLamb;
				damager.damage = 2;
				this.vel.x = xDir * 100;
				maxTime = 2f;
				maxDistance = 250;
				break;
			case (int)VileMissileType.SerotinalBullet:
				projId = (int)ProjIds.SerotinalBullet;
				damager.damage = 2;
				this.vel.x = xDir * 20;
				maxTime = 2f;
				maxDistance = 200;
				break;
		}
		if (rpc) {
			byte[] extraArgs = new byte[] { (byte)type };

			rpcCreate(pos, player, netProjId, xDir, extraArgs);
		}
	}
	public static Projectile rpcInvoke(ProjParameters args) {
		VileMissile vileMissile = VileMissile.netWeaponHC;
		switch (args.projId) {
			case (int)ProjIds.HumerusCrush:
				vileMissile = VileMissile.netWeaponHC;
				break;
			case (int)ProjIds.PopcornDemon:
				vileMissile = VileMissile.netWeaponPD;
				break;
			case (int)ProjIds.PopcornDemonSplit:
				vileMissile = VileMissile.netWeaponPD;
				break;
			case (int)ProjIds.BanzaiBeetle:
				vileMissile = VileMissile.netWeaponBB;
				break;
			case (int)ProjIds.LostLamb:
				vileMissile = VileMissile.netWeaponLL;
				break;
			case (int)ProjIds.SerotinalBullet:
				vileMissile = VileMissile.netWeaponSB;
				break;
		}
		return new VileMissileProj(
			vileMissile, args.pos, args.xDir, args.extraData[0], args.player, args.netId
		);
	}

	public override void update() {
		base.update();
		if (missileWeapon.type == (int)VileMissileType.SerotinalBullet && time > 0.5f) {
			if (MathF.Abs(vel.x) >= 400) {
				vel.x = 400 * xDir;
			} else {
				vel.x += 20 * xDir;
			}
		}
		if (!ownedByLocalPlayer) return;
		if (missileWeapon.type == (int)VileMissileType.PopcornDemon && type == 0 && !split) {
			if (time > 0.3f || owner.input.isPressed(Control.Special1, owner)) {
				split = true;
				playSound("vileMissile", sendRpc: true);
				destroySelfNoEffect();
				new VileMissileProj(missileWeapon, pos, xDir, 1, owner, owner.getNextActorNetId(), new Point(xDir, -1).normalize(), rpc: true);
				new VileMissileProj(missileWeapon, pos, xDir, 1, owner, owner.getNextActorNetId(), new Point(xDir, 0), rpc: true);
				new VileMissileProj(missileWeapon, pos, xDir, 1, owner, owner.getNextActorNetId(), new Point(xDir, 1).normalize(), rpc: true);
			}
		}
	}

	/*
	public override void onHitDamagable(IDamagable damagable)
	{
		base.onHitDamagable(damagable);

		if (damagable is Character character)
		{
			var victimCenter = character.getCenterPos();
			var bombCenter = pos;
			var dirTo = bombCenter.directionToNorm(victimCenter);
			character.vel.y = dirTo.y * 150;
			character.xPushVel = dirTo.x * 300;
		}
	}
	*/
}

public class VileMK2StunShot : Weapon {
	public VileMK2StunShot() : base() {
		fireRate = 45;
		index = (int)WeaponIds.MK2StunShot;
		killFeedIndex = 67;
	}

	public override void getProjectile(Point pos, int xDir, Player player, float chargeLevel, ushort netProjId) {
		new StunShotProj(this, pos, xDir, 0, player, netProjId);
	}
}

public class StunShotProj : Projectile {
	public StunShotProj(Weapon weapon, Point pos, int xDir, int type, Player player, ushort netProjId, Point? vel = null, bool rpc = false) :
		base(weapon, pos, xDir, 100, 0, player, type == 0 ? "vile_stun_shot" : "vile_ebomb_start", type == 0 ? Global.defFlinch : 0, 0.15f, netProjId, player.ownedByLocalPlayer) {
		fadeSprite = "vile_stun_shot_fade";
		fadeOnAutoDestroy = true;
		projId = (int)ProjIds.ElectricShock;
		maxTime = 0.75f;
		destroyOnHit = true;
		canBeLocal = false; // TODO: Remove the need for this.

		if (vel != null) {
			if (type == 0) {
				var norm = vel.Value.normalize();
				this.vel.x = norm.x * speed * player.character.getShootXDir();
				this.vel.y = norm.y * speed;
				this.vel.x *= 1.5f;
				this.vel.y *= 2f;
			} else {
				this.vel = vel.Value;
			}
		}

		if (type == 1) {
			damager.damage = 1;
		}

		if (rpc) {
			rpcCreate(pos, player, netProjId, xDir);
		}
	}

	public override void update() {
		base.update();
	}
}

public class VileMK2StunShotProj : Projectile {
	public VileMK2StunShotProj(Weapon weapon, Point pos, int xDir, Player player, ushort netProjId, Point? vel = null, bool rpc = false) :
		base(weapon, pos, xDir, 150, 1, player, "vile_stun_shot2", 0, 0.15f, netProjId, player.ownedByLocalPlayer) {
		fadeSprite = "vile_stun_shot_fade";
		projId = (int)ProjIds.MK2StunShot;
		maxTime = 0.75f;
		destroyOnHit = true;

		if (vel != null) {
			var norm = vel.Value.normalize();
			this.vel.x = norm.x * speed * player.character.getShootXDir();
			this.vel.y = norm.y * speed;
			this.vel.x *= 1.5f;
			if (player.character.charState is InRideArmor) this.vel.y *= 1.5f;
			else this.vel.y *= 2f;
			if (this.vel.y == 0) this.vel.y = 5;
		}

		if (rpc) {
			rpcCreate(pos, player, netProjId, xDir);
		}
	}

	public override void update() {
		base.update();
	}
}

public class MissileAttack : CharState {
	public MissileAttack() : base("idle_shoot", "", "", "") {
		exitOnAirborne = true;
		normalCtrl = true;
	}

	public override void update() {
		base.update();

		groundCodeWithMove();

		if (character.sprite.isAnimOver()) {
			character.changeToIdleOrFall();
		}
	}

	public static void shootLogic(Vile vile) {
		Player player = vile.player;
		bool isStunShot = vile.missileWeapon.type == (int)VileMissileType.ElectricShock;
		if (vile.sprite.getCurrentFrame().POIs.IsNullOrEmpty()) return;
		Point shootVel = vile.getVileShootVel(isStunShot);

		Point shootPos = vile.setCannonAim(shootVel);

		if (isStunShot) {
			new StunShotProj(vile.missileWeapon, shootPos, vile.getShootXDir(), 0, vile.player, vile.player.getNextActorNetId(), shootVel, rpc: true);
		} else {
			vile.playSound("vileMissile", sendRpc: true);
			new VileMissileProj(vile.missileWeapon, shootPos, vile.getShootXDir(), 0, vile.player, vile.player.getNextActorNetId(), shootVel, rpc: true);
		}
	}

	public static void mk2ShootLogic(Vile vile, bool isStunShot) {
		Player player = vile.player;
		Point? headPosNullable = vile.getVileMK2StunShotPos();
		if (headPosNullable == null) return;

		vile.playSound("mk2stunshot", sendRpc: true);
		new Anim(headPosNullable.Value, "dust", 1, vile.player.getNextActorNetId(), true, true);

		if (isStunShot) {
			new VileMK2StunShotProj(new VileMK2StunShot(), headPosNullable.Value, vile.getShootXDir(), vile.player, vile.player.getNextActorNetId(), vile.getVileShootVel(true), rpc: true);
		} else {
			new VileMissileProj(vile.missileWeapon, headPosNullable.Value, vile.getShootXDir(), 0, vile.player, vile.player.getNextActorNetId(), vile.getVileShootVel(false), rpc: true);
		}
	}

	public override void onEnter(CharState oldState) {
		base.onEnter(oldState);
		shootLogic(character as Vile ?? throw new NullReferenceException());
	}
}
