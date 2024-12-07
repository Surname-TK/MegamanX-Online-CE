using System;

namespace MMXOnline;

public class NovaStrike : Weapon {
	public const float ammoUsage = 14;
	public NovaStrike(Player? player) : base() {
		if (player != null) {
			int flinch = player.hasUltimateArmor() ? 0 : Global.halfFlinch;
			damager = new Damager(player, 2, flinch, 0.5f);
		}
		shootSounds = new string[] { "", "", "", "" };
		fireRate = 60;
		index = (int)WeaponIds.NovaStrike;
		weaponBarBaseIndex = 42;
		weaponBarIndex = 12;
		weaponSlotIndex = 95;
		killFeedIndex = 104;
		ammo = 28;
		drawGrayOnLowAmmo = false;
		drawRoundedDown = true;
		hasCustomAnim = true;
	}

	public override void shoot(Character character, int[] args) {
		if (character.ownedByLocalPlayer) {
			MegamanX mmx = character as MegamanX ?? throw new NullReferenceException();
			mmx.novaStrikeCooldown = fireRate;
			int level = mmx.novaStrikeLevel(ammo);

			character.changeState(new NovaStrikeState(level), true);
			addAmmo(-ammoUsage, mmx.player);
		}
		
	}

	public override float getAmmoUsage(int chargeLevel) {
		if (Global.level?.isHyper1v1() == true) {
			return 0;
		}
		return 0;
	}

	public override bool canShoot(int chargeLevel, Player player) {
		return player.character?.flag == null/*  && ammo >= (player.hasChip(3) ? ammoUsage / 2 : ammoUsage) */;
	}
}

public class NovaStrikeState : CharState {
	int upOrDown;
	int leftOrRight;
	int level;
	float[] speed = new float[] {120, 300, 420};
	public NovaStrikeState(int level) : base(getLevel(level), "", "", "nova_strike_start") {
		invincible = level >= 3;
		immuneToWind = level >= 2;
		superArmor = level >= 2;
		useDashJumpSpeed = true;
		normalCtrl = false;
		attackCtrl = false;
		useGravity = false;
		this.level = level;
	}

	public override void update() {
		base.update();

		if (!inTransition()) {
			if (!character.tryMove(new Point(character.xDir * speed[level - 1], 350 * upOrDown), out _) ||
				character.flag != null || stateTime > 0.6f
			) {
				character.changeToIdleOrFall();
				return;
			}
		}
	}

	public override void onEnter(CharState oldState) {
		base.onEnter(oldState);
		character.stopMoving();
		character.stopCharge();
		if (oldState is WallSlide) character.xDir *= -1;
	}

	public override void onExit(CharState newState) {
		base.onExit(newState);
		character.yDir = 1;
	}

	public override void onTransition() {
		string sound = level >= 3 ? "novaStrikeX6" : "novaStrikeX4";
		character.playSound(sound, sendRpc: true);
	}

	static string getLevel(int lv) {
		return lv switch {
			3 => "nova_strike_3",
			2 => "nova_strike_2",
			_ => "nova_strike"
		};
	}

}
