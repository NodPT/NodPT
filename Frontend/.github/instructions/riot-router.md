<app>
  <router>
    <!-- These links will trigger automatically HTML5 history events -->
    <nav>
      <a href="/home">Home</a>
      <a href="/about">About</a>
      <a href="/team/gianluca">Gianluca</a>
    </nav>

    <!-- Your application routes will be rendered here -->
    <route path="/home"> Home page </route>
    <route path="/about"> About </route>
    <route path="/team/:person"> Hello dear { route.params.person } </route>
  </router>

  <script>
    import { Router, Route } from '@riotjs/route'

    export default {
      components { Router, Route }
    }
  </script>
</app>

import { route, router, setBase } from '@riotjs/route'

// required to set base first
setBase('/')

// create a route stream
const aboutStream = router.push('/about')

aboutStream.on.value((url) => {
  console.log(url) // URL object
})

aboutStream.on.value(() => {
  console.log('just log that the about route was triggered')
})

// triggered on each route event
router.on.value((path) => {
  // path is always a string in this function
  console.log(path)
})

// trigger a route change manually
router.push('/about')

// end the stream
aboutStream.end()