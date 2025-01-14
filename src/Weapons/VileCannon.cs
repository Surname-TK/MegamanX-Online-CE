using System;
using System.Linq;

namespace MMXOnline;

public enum VileCannonType {
	None = -1,
	FrontRunner,
	TridentLine,
	FatBoy,
	FireMurrain,
	LongshotGizmo
}

public class VileCannon : Weapon {
	public static VileCannon netWeaponFR = new VileCannon(VileCannonType.FrontRunner);
	public static VileCannon netWeaponTL = new VileCannon(VileCannonType.TridentLine);
	public static VileCannon netWeaponFB = new VileCannon(VileCannonType.FatBoy);
	public static VileCannon netWeaponFM = new VileCannon(VileCannonType.FireMurrain);
	public static VileCannon netWeaponLG = new VileCannon(VileCannonType.LongshotGizmo);
	public string projSprite = "";
	public string fadeSprite = "";
	public float vileAmmoUsage;

	public VileCannon(VileCannonType vileCannonType) : base() {
		index = (int)WeaponIds.FrontRunner;
		weaponBarBaseIndex = 56;
		weaponBarIndex = 56;
		killFeedIndex = 56;
		weaponSlotIndex = 43;
		type = (int)vileCannonType;
		switch (vileCannonType) {
			case VileCannonType.None:
				displayName = "None";
				description = new string[] { "Do not equip a cannon." };
				killFeedIndex = 126;
				break;
			case VileCannonType.FrontRunner:
				fireRate = 30;
				vileAmmoUsage = 7;
				displayName = "Front Runner";
				projSprite = "vile_mk2_proj";
				fadeSprite = "vile_mk2_proj_fade";
				description = new string[] { "This cannon not only offers power,", "but can be aimed up and down." };
				vileWeight = 2;
				break;
				case VileCannonType.TridentLine:
				break;
			case VileCannonType.FatBoy:
				fireRate = 30;
				vileAmmoUsage = 21;
				displayName = "Fat Boy";
				projSprite = "vile_mk2_fb_proj";
				fadeSprite = "vile_mk2_fb_proj_fade";
				killFeedIndex = 90;
				weaponSlotIndex = 61;
				description = new string[] { "The most powerful cannon around,", "it consumes a lot of energy." };
				vileWeight = 3;
				break;
				case VileCannonType.FireMurrain:
				break;
			case VileCannonType.LongshotGizmo:
				fireRate = 6;
				vileAmmoUsage = 4;
				displayName = "Longshot Gizmo";
				projSprite = "vile_mk2_lg_proj";
				fadeSprite = "vile_mk2_lg_proj_fade";
				killFeedIndex = 91;
				weaponSlotIndex = 62;
				description = new string[] { "This cannon fires 5 shots at once,", "but leaves you open to attack." };
				vileWeight = 4;
				break;
		}
	}

	public override void vileShoot(WeaponIds weaponInput, Vile vile) {
		bool isLongshotGizmo = type == (int)VileCannonType.LongshotGizmo;
		if (isLongshotGizmo && vile.gizmoCooldown > 0) return;

		Player player = vile.player;
		if (shootCooldown > 0 || !vile.missileWeapon.isCooldownPercentDone(0.8f)) return;
		if (vile.charState is MissileAttack || vile.charState is RocketPunchAttack) return;
		float overrideAmmoUsage = vileAmmoUsage;

		if (isLongshotGizmo && vile.longshotGizmoCount > 0) {
			vile.usedAmmoLastFrame = true;
			if (vile.weaponHealAmount == 0) {
				player.vileAmmo -= vileAmmoUsage;
				if (player.vileAmmo < 0) player.vileAmmo = 0;
			}
		} else if (!vile.tryUseVileAmmo(overrideAmmoUsage)) return;

		if (isLongshotGizmo && player.vileAmmo >= 22) {
			vile.isShootingLongshotGizmo = true;
		}

		bool gizmoStart = (isLongshotGizmo && vile.charState is not CannonAttack);
		if (vile.longshotGizmoCount == 0 && (gizmoStart || vile.charState is Idle || vile.charState is Run || vile.charState is Dash || vile.charState is VileMK2GrabState)) {
			//if (player.vileAmmo >= 22) {
				vile.setVileShootTime(this);
				vile.changeState(new CannonAttack(isLongshotGizmo, vile.grounded), true);
			//}
		} else {
			if (vile.charState is LadderClimb) {
				if (player.input.isHeld(Control.Left, player)) vile.xDir = -1;
				if (player.input.isHeld(Control.Right, player)) vile.xDir = 1;
				vile.changeSpriteFromName("ladder_shoot2", true);
			}

			if (vile.charState is Jump || vile.charState is Fall || vile.charState is WallKick || vile.charState is VileHover || vile.charState is AirDash) {
				vile.setVileShootTime(this);
				if (!Options.main.lockInAirCannon) {
					if (vile.charState is AirDash) {
						vile.changeState(new Fall(), true);
					}
					vile.changeSpriteFromName("cannon_air", true);
					CannonAttack.shootLogic(vile);
				} else {
					vile.changeState(new CannonAttack(false, false), true);
				}
			} else {
				vile.setVileShootTime(this);
				CannonAttack.shootLogic(vile);
			}
		}

		if (isLongshotGizmo) {
			vile.longshotGizmoCount++;
			if (vile.longshotGizmoCount >= 5 || player.vileAmmo <= 3) {
				vile.longshotGizmoCount = 0;
				vile.isShootingLongshotGizmo = false;
			}
		}
	}
}

public class VileCannonProj : Projectile {
	public VileCannonProj(
		VileCannon weapon, Point pos, float byteAngle, Player player,
		ushort netProjId, bool rpc = false
	) : base(
		weapon, pos, 1, 300, 2, player, weapon.projSprite, 0, 0f, netProjId, player.ownedByLocalPlayer
	) {
		fadeSprite = weapon.fadeSprite;
		fadeOnAutoDestroy = true;
		projId = (int)ProjIds.FrontRunner;
		maxTime = 0.5f;
		destroyOnHit = true;
		//destroyOnHitWall = true;
		switch (weapon.type){
			case (int)VileCannonType.FrontRunner:
				projId = (int)ProjIds.FrontRunner;
				// Nothing.
				break;
			case (int)VileCannonType.TridentLine:
				projId = (int)ProjIds.TridentLine;
				break;
			case (int)VileCannonType.FatBoy:
				projId = (int)ProjIds.FatBoy;
				damager = new Damager(owner, 3, Global.halfFlinch, 0);
				xScale = xDir;
				maxTime = 0.45f;
				break;
			case (int)VileCannonType.FireMurrain:
				projId = (int)ProjIds.FireMurrain;
				break;
			case (int)VileCannonType.LongshotGizmo:
				projId = (int)ProjIds.LongshotGizmo;
				damager = new Damager(owner, 0.5f, Global.miniFlinch, 0);
				break;
		}
		// Speed and angle.
		Point norm = Point.createFromByteAngle(byteAngle);
		this.vel.x = norm.x * speed * xDir;
		this.vel.y = norm.y * speed;
		this.byteAngle = byteAngle;

		if (rpc) {
			rpcCreateByteAngle(pos, player, netProjId, byteAngle);
		}
	}

	public static Projectile rpcInvoke(ProjParameters args) {
		VileCannon vileCannon = VileCannon.netWeaponFR;;
		switch (args.projId) {
			case (int)ProjIds.FrontRunner:
				vileCannon = VileCannon.netWeaponFR;
				break;
			case (int)ProjIds.TridentLine:
				vileCannon = VileCannon.netWeaponTL;
				break;
			case (int)ProjIds.FatBoy:
				vileCannon = VileCannon.netWeaponFB;
				break;
			case (int)ProjIds.FireMurrain:
				vileCannon = VileCannon.netWeaponFM;
				break;
			case (int)ProjIds.LongshotGizmo:
				vileCannon = VileCannon.netWeaponLG;
				break;
		}
		return new VileCannonProj(
			vileCannon, args.pos, args.byteAngle, args.player, args.netId
		);
	}
}

public class CannonAttack : CharState {
	bool isGizmo;
	private Vile vile = null!;

	public CannonAttack(bool isGizmo, bool grounded) : base(getSprite(isGizmo, grounded), "", "", "") {
		this.isGizmo = isGizmo;
		useDashJumpSpeed = true;
	}

	public static string getSprite(bool isGizmo, bool grounded) {
		if (isGizmo) {
			return grounded ? "idle_gizmo" : "cannon_gizmo_air";
		}
		return grounded ? "idle_shoot" : "cannon_air";
	}

	public override void update() {
		base.update();

		if (vile.isShootingLongshotGizmo) {
			if (vile.cannonWeapon.shootCooldown == 0) {
				vile.cannonWeapon.vileShoot(0, vile);
			}
			if (player.vileAmmo <= 0) {
				vile.isShootingLongshotGizmo = false;
			}
			return;
		}
		//groundCodeWithMove();

		if (character.sprite.isAnimOver()) {
			character.changeToIdleOrFall();
		}
	}

	public static void shootLogic(Vile vile) {
		if (vile.sprite.getCurrentFrame().POIs.IsNullOrEmpty()) {
			return;
		}
		Point shootVel = vile.getVileShootVel(true);

		var player = vile.player;
		vile.playSound("frontrunner", sendRpc: true);

		string muzzleSprite = "cannon_muzzle";
		if (vile.cannonWeapon.type == (int)VileCannonType.FatBoy) muzzleSprite += "_fb";
		if (vile.cannonWeapon.type == (int)VileCannonType.LongshotGizmo) muzzleSprite += "_lg";

		Point shootPos = vile.setCannonAim(new Point(shootVel.x, shootVel.y));
		if (vile.sprite.name.EndsWith("_grab")) {
			shootPos = vile.getFirstPOIOrDefault("s");
		}

		var muzzle = new Anim(
			shootPos, muzzleSprite, vile.getShootXDir(), player.getNextActorNetId(), true, true, host: vile
		);
		muzzle.angle = new Point(shootVel.x, vile.getShootXDir() * shootVel.y).angle;
		if (vile.getShootXDir() == -1) {
			shootVel = new Point(shootVel.x * vile.getShootXDir(), shootVel.y);
		}

		new VileCannonProj(
			vile.cannonWeapon,
			shootPos, MathF.Round(shootVel.byteAngle), //vile.longshotGizmoCount,
			player, player.getNextActorNetId(), rpc: true
		);
	}

	public override void onEnter(CharState oldState) {
		base.onEnter(oldState);
		vile = character as Vile ?? throw new NullReferenceException();
		shootLogic(vile);
		character.useGravity = false;
		character.stopMoving();
	}

	public override void onExit(CharState newState) {
		base.onExit(newState);
		vile.isShootingLongshotGizmo = false;
		character.useGravity = true;
		if (isGizmo) {
			vile.gizmoCooldown = 0.5f;
		}
	}
}
