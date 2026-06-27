const fs=require('fs');

// 등급 -> 강화석 base (기획서: 레어10/에픽100/레전더리300)
const stoneBase = { Rare:10, Epic:100, Legendary:300 };
const master = JSON.parse(fs.readFileSync('Assets/Resources/Data/Tables/gear_master.json','utf8'));
const grade = {};
for(const g of master.data) grade[g.Gear_ID] = g.GearGrade;

// ---------- 1) SO (.asset, 런타임 소스) ----------
const soPath = 'Assets/_Project/Data/SO/GearUpgradeCostTable.asset';
let so = fs.readFileSync(soPath,'utf8');
const eol = so.includes('\r\n') ? '\r\n' : '\n';
// 골드 행 블록 파싱
const blockRe = /  - Gear_ID: (\d+)\r?\n    StartLevel: (\d+)\r?\n    EndLevel: (\d+)\r?\n    Type: 70001\r?\n    BaseAmount: (\d+)\r?\n    GrowthValue: ([\d.]+)/g;
let m, stoneRows=[], n=0;
while((m=blockRe.exec(so))){
  const id=+m[1], start=+m[2], end=+m[3], growth=m[5];
  const gr=grade[id];
  const base=stoneBase[gr];
  if(base==null){ console.log('grade unknown for', id); continue; }
  stoneRows.push(
    `  - Gear_ID: ${id}`,
    `    StartLevel: ${start}`,
    `    EndLevel: ${end}`,
    `    Type: 70004`,
    `    BaseAmount: ${base}`,
    `    GrowthValue: ${growth}`
  );
  n++;
}
if(!so.endsWith(eol)) so+=eol;
so += stoneRows.join(eol)+eol;
fs.writeFileSync(soPath, so);
console.log('SO: 강화석 행', n, '개 추가');

// ---------- 2) JSON (Resources, 파이프라인 일관성) ----------
const jsonPath = 'Assets/Resources/Data/Tables/gear_upgrade_cost.json';
const j = JSON.parse(fs.readFileSync(jsonPath,'utf8'));
const goldRows = j.data.filter(r=>r.Type===70001);
let jn=0;
for(const r of goldRows){
  const base=stoneBase[grade[r.Gear_ID]];
  if(base==null) continue;
  j.data.push({ '#Type': r['#Type'], Gear_ID:r.Gear_ID, StartLevel:r.StartLevel, EndLevel:r.EndLevel, Type:70004, BaseAmount:base, GrowthValue:r.GrowthValue });
  jn++;
}
fs.writeFileSync(jsonPath, JSON.stringify(j, null, 2));
console.log('JSON: 강화석 행', jn, '개 추가');
