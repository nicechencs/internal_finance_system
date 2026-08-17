import { chromium } from "playwright"

const baseUrl = 'http://127.0.0.1:5173'
const creds = { username: 'admin', password: 'DemoOnly_ChangeMe!' }

async function run() {
  const browser = await chromium.launch({ headless: true })
  const page = await browser.newPage()
  page.on('console', (msg) => {
    console.log('[browser]', msg.type(), msg.text())
  })

  await page.goto(`${baseUrl}/login`, { waitUntil: 'networkidle' })
  const inputs = page.locator('input')
  await inputs.nth(0).fill(creds.username)
  await inputs.nth(1).fill(creds.password)
  await page.locator('.login-btn').click()
  await page.waitForURL('**/', { waitUntil: 'networkidle' })
  console.log('After login URL:', page.url())

  const menus = await page.evaluate(() =>
    Array.from(document.querySelectorAll('.el-menu-item')).map((el) => ({
      index: el.getAttribute('data-index'),
      text: el.textContent?.trim()
    }))
  )
  console.log('Menus:', menus)

  const clickMenu = async (index) => {
    console.log(`Clicking menu ${index}`)
    await page.locator(`.el-menu-item[data-index="${index}"]`).click()
    await page.waitForTimeout(1500)
    console.log('Current URL:', page.url())
  }

  await clickMenu('/accounts')
  await clickMenu('/categories')
  await clickMenu('/transactions')

  await browser.close()
}

run().catch((err) => {
  console.error(err)
  process.exit(1)
})
