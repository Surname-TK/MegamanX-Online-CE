namespace MMXOnline;

public enum VulcanType {
	None = -1,
	CherryBlast,
	ZipZapper,
	BuckshotDance,
	DistanceNeedler,
	TripleSeven
}

public class Vulcan : Weapon {
	public static Vulcan netWeaponCB = new Vulcan(VulcanType.CherryBlast);
	public static Vulcan netWeaponZZ = new Vulcan(VulcanType.ZipZapper);
	public static Vulcan netWeaponBD = new Vulcan(VulcanType.BuckshotDance);
	public static Vulcan netWeaponDN = new Vulcan(VulcanType.DistanceNeedler);
	public static Vulcan netWeapon777 = new Vulcan(VulcanType.TripleSeven);
	public string muzzleSprite;
	public string projSprite;
	public float vileAmmoUsage;

	public Vulcan(VulcanType vulcanType) : base() {
		index = (int)WeaponIds.Vulcan;
		weaponBarBaseIndex = 26;
		weaponBarIndex = weaponBarBaseIndex;
		killFeedIndex = 62;
		weaponSlotIndex = 44;
		type = (int)vulcanType;

		switch (vulcanType) {
			case VulcanType.None:
				displayName = "None";
				description = new string[] { "Do not equip a Vulcan." };
				killFeedIndex = 126;
				break;
			case VulcanType.CherryBlast:
				fireRate = 10;
				displayName = "Cherry Blast";
				vileAmmoUsage = 1f;
				muzzleSprite = "vulcan_cb_muzzle";
				projSprite = "vulcan_cb_proj";
				description = new string[] { "With a range of approximately 20 feet,", "this vulcan is easy to use." };
				vileWeight = 2;
				break;
			case VulcanType.ZipZapper:
				fireRate = 6;
				displayName = "Zip Zapper";
				vileAmmoUsage = 2;
				muzzleSprite = "vulcan_zz_muzzle";
				projSprite = "vulcan_zz_proj";
				description = new string[] { "Though boasting extreme accuracy,", "this vulcan lacks extensive range." };
				killFeedIndex = 181;
				vileWeight = 3;
				break;
			case VulcanType.BuckshotDance:
				fireRate = 10;
				displayName = "Buckshot Dance";
				vileAmmoUsage = 3f;
				muzzleSprite = "vulcan_bd_muzzle";
				projSprite = "vulcan_bd_proj";
				killFeedIndex = 89;
				weaponSlotIndex = 60;
				description = new string[] { "The scattering power of this vulcan", "results in less than perfect aiming." };
				vileWeight = 4;
				break;
			case VulcanType.DistanceNeedler:
				fireRate = 20;
				displayName = "Distance Needler";
				vileAmmoUsage = 4;
				muzzleSprite = "vulcan_dn_muzzle";
				projSprite = "vulcan_dn_proj";
				killFeedIndex = 88;
				weaponSlotIndex = 59;
				description = new string[] { "This vulcan has good range and speed,", "but cannot fire rapidly." };
				vileWeight = 2;
				break;
			case VulcanType.TripleSeven:
				fireRate = 6;
				displayName = "Triple 7";
				vileAmmoUsage = 2;
				muzzleSprite = "vulcan_777_muzzle";
				projSprite = "vulcan_777_proj";
				killFeedIndex = 182;
				weaponSlotIndex = 59;
				description = new string[] { "Boasting both power and speed,", "this vulcan consumes much power." };
				vileWeight = 2;
				break;
		}
	}

	public override void vileShoot(WeaponIds weaponInput, Vile vile) {
		if (shootCooldown > 0) return;
		if (string.IsNullOrEmpty(vile.charState.shootSprite)) return;

		Player player = vile.player;
		if (vile.tryUseVileAmmo(vileAmmoUsage)) {
			if (vile.charState is LadderClimb) {
				if (player.input.isHeld(Control.Left, player)) vile.xDir = -1;
				if (player.input.isHeld(Control.Right, player)) vile.xDir = 1;
			}
			vile.changeSpriteFromName(vile.charState.shootSprite, false);
			shootVulcan(vile);
		}
	}

	public void shootVulcan(Vile vile) {
		Player player = vile.player;
		if (shootCooldown <= 0) {
			vile.vulcanLingerTime = 0f;
			new VulcanMuzzleAnim(this, vile.getShootPos(), vile.getShootXDir(), vile, player.getNextActorNetId(), true, true);
			new VulcanProj(this, vile.getShootPos(), vile.getShootXDir(), player, player.getNextActorNetId(), rpc: true);
			if (type == (int)VulcanType.BuckshotDance && Global.isOnFrame(3)) {
				new VulcanProj(this, vile.getShootPos(), vile.getShootXDir(), player, player.getNextActorNetId(), rpc: true);
			}
			vile.playSound("vulcan", sendRpc: true);
			shootCooldown = fireRate;
		}
	}
}

public class VulcanProj : Projectile {
	public VulcanProj(Vulcan weapon, Point pos, int xDir, Player player, ushort netProjId, bool rpc = false) :
		base(weapon, pos, xDir, 400, 1, player, weapon.projSprite, 0, 0f, netProjId, player.ownedByLocalPlayer) {
		projId = (int)ProjIds.Vulcan;
		maxTime = 0.4f;
		destroyOnHit = true;
		destroyOnHitWall = true;
		reflectable = true;

		switch (weapon.type) {
			case (int)VulcanType.CherryBlast:
				projId = (int)ProjIds.CherryBlast;
				break;
			case (int)VulcanType.ZipZapper:
				projId = (int)ProjIds.ZipZapper;
				maxTime = 0.2f;
				break;
			case (int)VulcanType.BuckshotDance:
				projId = (int)ProjIds.BuckshotDance;
				int rand = 0;
				float angle = 0;
				if (player.character is Vile vile) {
					rand = vile.buckshotDanceNum % 5;
					vile.buckshotDanceNum++;
				}
				if (rand == 0) angle = 0;
				if (rand == 1) angle = -10;
				if (rand == 2) angle = 10;
				if (rand == 3) angle = -20;
				if (rand == 4) angle = 20;
				if (xDir == -1) angle += 180;
				vel = Point.createFromAngle(angle).times(speed);
				break;
			case (int)VulcanType.DistanceNeedler:
				projId = (int)ProjIds.DistanceNeedler;
				damager = new Damager(owner, 2, 0, 0.2f);
				maxTime = 0.5f;
				vel.x = 600 * xDir;
				reflectable = false;
				destroyOnHit = false;
				break;
			case (int)VulcanType.TripleSeven:
				projId = (int)ProjIds.TripleSeven;
				int rand7 = 0;
				float angle7 = 0;
				if (player.character is Vile vile7) {
					rand7 = vile7.tripleSevenNum % 5;
					vile7.tripleSevenNum++;
				}
				if (rand7 == 0) angle7 = 0;
				if (rand7 == 1) angle7 = -10;
				if (rand7 == 2) angle7 = 10;
				if (rand7 == 3) angle7 = -20;
				if (rand7 == 4) angle7 = 20;
				if (xDir == -1) angle7 += 180;
				vel = Point.createFromAngle(angle7).times(speed);
				maxTime = 0.5f;
				break;
		}

		if (rpc) {
			rpcCreate(pos, player, netProjId, xDir);
		}
	}
	public static Projectile rpcInvoke(ProjParameters args) {
		Vulcan vulcan = Vulcan.netWeaponCB;
		switch (args.projId){
			case (int)ProjIds.CherryBlast:
				vulcan = Vulcan.netWeaponCB;
				break;
			case (int)ProjIds.ZipZapper:
				vulcan = Vulcan.netWeaponZZ;
				break;
			case (int)ProjIds.BuckshotDance:
				vulcan = Vulcan.netWeaponBD;
				break;
			case (int)ProjIds.DistanceNeedler:
				vulcan = Vulcan.netWeaponDN;
				break;
			case (int)ProjIds.TripleSeven:
				vulcan = Vulcan.netWeapon777;
				break;
		}
		return new VulcanProj(
			vulcan, args.pos, args.xDir, args.player, args.netId
		);
	}
	public override void update() {
		base.update();
	}
}

public class VulcanMuzzleAnim : Anim {
	Character chr;
	public VulcanMuzzleAnim(Vulcan weapon, Point pos, int xDir, Character chr, ushort? netId = null, bool sendRpc = false, bool ownedByLocalPlayer = true) :
		base(pos, weapon.muzzleSprite, xDir, netId, true, sendRpc, ownedByLocalPlayer) {
		this.chr = chr;
	}

	public override void postUpdate() {
		if (chr.currentFrame.getBusterOffset() != null) {
			changePos(chr.getShootPos());
		}
	}
}

public class VulcanCharState : CharState {
	bool isCrouch;
	public VulcanCharState(bool isCrouch) : base(isCrouch ? "crouch_shoot" : "idle_shoot", "", "", "") {
		useDashJumpSpeed = true;
		this.isCrouch = isCrouch;
	}

	public override void update() {
		base.update();

		if (isCrouch && !player.input.isHeld(Control.Down, player)) {
			character.changeToIdleOrFall();
			return;
		}

		if (!player.input.isHeld(Control.Shoot, player) || !(player.weapon is Vulcan)) {
			if (isCrouch) {
				character.changeToCrouchOrFall();
			} else {
				character.changeToIdleOrFall();
			}
			return;
		}

		if (player.input.isHeld(Control.Left, player)) character.xDir = -1;
		if (player.input.isHeld(Control.Right, player)) character.xDir = 1;
	}
}
