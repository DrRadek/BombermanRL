# Požadavky na trénování (jiné verze mohou, ale nemusí fungovat)
- Linux
- Python 3.10
- Godot RL Agents (verze 0.6.2 se Sample Factory frameworkem)
    - `pip install godot-rl[sf]==0.6.2`
    - viz https://github.com/edbeeching/godot_rl_agents/blob/main/docs/ADV_SAMPLE_FACTORY.md
# Dodatečné soubory
- Dodatečné soubory lze stáhnout [zde](https://drive.google.com/drive/folders/1zQ3qfWCtoLY8OpH5WwKn0YjqcTZzlD9t?usp=drive_link)
    - Ve složce Models se nachází naučené modely
    - Ve složce Game Export se nachází aktuální export hry spolu se souborem `godot_env.py` potřebným v kroku 3
    - Ve složce Results se nachází skript pro zpracování dat o odehraných hrách spolu s daty
# Postup při trénování
1. Exportovat hru (vyžaduje godot 4)
    - Exportovanou hru lze stáhnout  [zde](https://drive.google.com/drive/folders/1mL_QtEDF6KmpBzmXMCYerj6qoZMjNLjY?usp=drive_link)
3. Úprava Godot RL Agents (GDRL)
    - Používám upravenou verzi GDRL
    - [Zde](https://drive.google.com/drive/folders/1mL_QtEDF6KmpBzmXMCYerj6qoZMjNLjY?usp=drive_link) se nachází soubor `godot_env.py`, nahraďte jím soubor umístěný v knihovnách pythonu: `python3.x/dist-packages/godot_rl/core/godot_env.py`
4. Příkazem `gdrl` spustíte učení
    - Parametry z GDRL: příkaz `gdrl -h`
    - Parametry ze Sample Factory: https://www.samplefactory.dev/02-configuration/cfg-params
    - Příklad: `gdrl --experiment=VsDecisionTreeV4 --trainer=sf --env=gdrl --env_path=./BombermanRL --speedup=4 --batched_sampling=False --device=cpu --train_for_env_steps=100000000 --num_workers=4 --recurrence=128 --rollout=128 --use_rnn=True --num_policies=2 --pbt_mix_policies_in_one_env=True --viz`

5. Spuštění již naučeného modelu
    - Export modelu momentálně není podporovaný, použijte `--eval` u `gdrl`
    - Příklad: `gdrl --experiment=VsDecisionTreeV4 --trainer=sf --env=gdrl --env_path=./BombermanRL --speedup=1 --batched_sampling=True --device=cpu --num_workers=1 --recurrence=128 --use_rnn=True --viz --eval`

